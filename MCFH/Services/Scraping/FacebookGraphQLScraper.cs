using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// Phase 1: Capture GraphQL JSON responses while browsing Facebook group feed.
/// Parses only the <b>feed listing</b> (GroupFeed / CometFeedRoot) to extract post stubs.
/// Phase 2 will add post detail + comment parsing via Feedback / CommentListComponentsQuery.
/// </summary>
public class FacebookGraphQLScraper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Scrape group feed by capturing GraphQL JSON responses from the browser.
    /// This runs alongside a Playwright page — the browser fires GraphQL requests as the user scrolls,
    /// and we intercept those responses without parsing any DOM.
    /// </summary>
    /// <param name="page">An already-opened, logged-in Playwright page pointing at the group URL.</param>
    /// <param name="maxPosts">Stop once we have at least this many unique posts.</param>
    /// <param name="options">Scrape options for headless / fast-demo mode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of posts parsed from GraphQL JSON, ordered by appearance.</returns>
    public async Task<List<GroupPost>> ScrapeFeedAsync(
        IPage page, int maxPosts, ScrapeOptions? options = null, CancellationToken ct = default)
    {
        options ??= new ScrapeOptions();
        var fast = options.FastDemoMode;

        // ── Phase 1: Capture all GraphQL responses ─────────────────────────────────
        var graphqlResponses = new ConcurrentBag<string>();
        var seenUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var totalResponses = 0;
        var filteredResponses = 0;

        page.Response += async (_, response) =>
        {
            if (ct.IsCancellationRequested) return;
            totalResponses++;

            if (!response.Url.Contains("/api/graphql/")) return;

            // Skip known noisy endpoints
            var url = response.Url;
            if (IsNoisyEndpoint(url)) return;

            // De-dupe by URL (same query fired twice → only keep first)
            if (!seenUrls.TryAdd(url, 0)) return;

            try
            {
                // Wait for response body to fully download before reading
                try { await response.FinishedAsync(); } catch { }
                var body = await response.TextAsync();
                filteredResponses++;

                // Skip empty bodies (e.g., 204 No Content, aborted requests)
                if (string.IsNullOrWhiteSpace(body) || body.Length < 100)
                {
                    return;
                }

                // Save debug FIRST so we can inspect even unparseable bodies
                try
                {
                    var debugDir = Path.GetTempPath();
                    var debugPath = Path.Combine(debugDir, $"fb_gql_resp_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
                    File.WriteAllText(debugPath, body);
                    Console.WriteLine($"[FB GraphQL] DEBUG: saved response ({body.Length} chars) → {debugPath}");
                }
                catch { /* debug-only, ignore */ }

                // Try parse — log specific failure for diagnosis
                try
                {
                    using var probe = JsonDocument.Parse(body);
                    if (ContainsFeedData(body))
                        graphqlResponses.Add(body);
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"[FB GraphQL] Skipped unparseable response ({body.Length} chars): {jex.Message}");
                    Console.WriteLine($"[FB GraphQL] Last 200 chars: {body[^Math.Min(200, body.Length)..]}");
                    // Recovery attempt: try parsing the prefix that is balanced (rare)
                    var trimmed = TryRecoverTruncatedJson(body);
                    if (trimmed != null)
                    {
                        try
                        {
                            using var probe2 = JsonDocument.Parse(trimmed);
                            if (ContainsFeedData(trimmed))
                                graphqlResponses.Add(trimmed);
                            Console.WriteLine($"[FB GraphQL] Recovered {trimmed.Length} chars from truncated response.");
                        }
                        catch { /* give up */ }
                    }
                }
            }
            catch
            {
                // Ignore read failures — the browser still renders, we just skip this response.
            }
        };

        // Also listen on POST requests: when user clicks a post, FB fires
        // CometSinglePostDialogContentQuery as POST. We capture request body to filter.
        var seenPostUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        page.Request += async (_, request) =>
        {
            if (ct.IsCancellationRequested) return;
            if (request.Method != "POST") return;
            if (!request.Url.Contains("/api/graphql/")) return;

            try
            {
                var postData = request.PostData;
                if (string.IsNullOrEmpty(postData)) return;
                if (!postData.Contains("CometSinglePostDialogContentQuery")) return;

                Console.WriteLine($"[FB GraphQL] Intercepted POST CometSinglePostDialogContentQuery");
                // Wait briefly for response — capture via Response handler that already exists
                // We'll store URL to filter later
            }
            catch { }
        };

        Console.WriteLine("[FB GraphQL] Response interceptor registered. Waiting for initial load...");

        // Wait for initial GraphQL responses to arrive before scrolling
        await page.WaitForTimeoutAsync(fast ? 3000 : 4000);
        Console.WriteLine($"[FB GraphQL] After initial wait: total={totalResponses}, filtered={filteredResponses}, feedMatch={graphqlResponses.Count}");

        // ── Phase 2: Scroll the feed to trigger GraphQL pagination ─────────────────
        var maxScrolls = fast ? 6 : 12;
        var scrollWaitMs = fast ? 1500 : 1200;

        Console.WriteLine($"[FB GraphQL] Starting feed capture. Target: {maxPosts} posts.");

        for (int i = 0; i < maxScrolls && !ct.IsCancellationRequested; i++)
        {
            var before = graphqlResponses.Count;
            await page.Mouse.WheelAsync(0, fast ? 2000 : 1500);
            await page.WaitForTimeoutAsync(scrollWaitMs);
            var after = graphqlResponses.Count;
            Console.WriteLine($"[FB GraphQL] Scroll {i + 1}/{maxScrolls}: +{after - before} new responses (total captured: {after})");
            Console.WriteLine($"[FB GraphQL]   [stats] total HTTP={totalResponses}, GQL-filtered={filteredResponses}, feedMatch={graphqlResponses.Count}");

            // Stop only if we've captured at least 1 response and got 4 consecutive zero-scrolls
            if (after > 0 && after - before == 0 && i >= 3)
            {
                Console.WriteLine("[FB GraphQL] No new responses in 4 consecutive scrolls — stopping.");
                break;
            }
            // Stop if we've done enough scrolls even with no responses
            if (after - before == 0 && i >= 7)
            {
                Console.WriteLine("[FB GraphQL] Max scrolls reached with no new responses.");
                break;
            }
        }

        // DEBUG: Save first response to file so we can see the actual schema
        var responsesArray = graphqlResponses.ToArray();
        if (responsesArray.Length > 0)
        {
            var first = responsesArray[0];
            var debugPath = Path.Combine(
                Path.GetTempPath(),
                $"fb_graphql_debug_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(debugPath, first);
            Console.WriteLine($"[FB GraphQL] DEBUG: saved first response ({first.Length} chars) to {debugPath}");
        }

        Console.WriteLine($"[FB GraphQL] Captured {graphqlResponses.Count} feed-related responses.");

        // ── Phase 3: Parse each response into GroupPost stubs ────────────────────
        var posts = new List<GroupPost>();
        var seenPostIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Phase 2b: Collect candidate post URLs from feed DOM ──────────────────
        // FB group feed has post links like https://www.facebook.com/groups/<gid>/posts/<postid>/
        // We grab them from <a> elements so we can click each and capture POST responses.
        var candidatePostUrls = new List<string>();
        try
        {
            var hrefs = await page.EvaluateAsync<List<string>>(@"
                () => {
                    const links = Array.from(document.querySelectorAll('a[href]'));
                    return links
                        .map(a => a.href)
                        .filter(h => /\/groups\/[^/]+\/posts\//.test(h));
                }
            ");
            foreach (var h in hrefs)
            {
                var normalized = NormalizeFacebookUrl(h);
                if (!string.IsNullOrWhiteSpace(normalized) && seenPostIds.Add(ExtractPostId(normalized)))
                    candidatePostUrls.Add(normalized);
                if (candidatePostUrls.Count >= maxPosts * 2) break; // grab a few extra, dedupe later
            }
            Console.WriteLine($"[FB GraphQL] Found {candidatePostUrls.Count} candidate post URLs in feed DOM.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FB GraphQL] Failed to collect post URLs: {ex.Message}");
        }

        // ── Phase 2c: Click each post to trigger CometSinglePostDialogContentQuery ─
        var dialogResponses = new ConcurrentBag<string>();
        var dialogActive = new System.Threading.ManualResetEventSlim(false);

        // Register a secondary response handler that ONLY captures dialog content queries
        async void OnDialogResponse(object? sender, IResponse response)
        {
            if (ct.IsCancellationRequested) return;
            if (!response.Url.Contains("/api/graphql/")) return;

            try
            {
                var body = await response.TextAsync();
                // Only keep responses from the single-post dialog query
                if (body.Contains("CometSinglePostDialogContentQuery", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("\"feedback\":", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("\"owning_profile\"", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("comet_sections", StringComparison.OrdinalIgnoreCase))
                {
                    dialogResponses.Add(body);
                }
            }
            catch { }
        }

        // Intercept POST requests so we know when CometSinglePostDialogContentQuery fires
        var dialogPostsInFlight = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        void OnDialogRequest(object? sender, IRequest request)
        {
            if (request.Method != "POST") return;
            if (!request.Url.Contains("/api/graphql/")) return;

            try
            {
                var pd = request.PostData;
                if (string.IsNullOrEmpty(pd)) return;
                if (pd.Contains("CometSinglePostDialogContentQuery"))
                {
                    Console.WriteLine($"[FB GraphQL] >>> Detected POST CometSinglePostDialogContentQuery");
                    dialogPostsInFlight.TryAdd(request.Url, 0);
                }
            }
            catch { }
        }

        page.Response += OnDialogResponse;
        page.Request += OnDialogRequest;

        int clickedCount = 0;
        foreach (var postUrl in candidatePostUrls)
        {
            if (posts.Count >= maxPosts) break;
            if (ct.IsCancellationRequested) break;

            try
            {
                Console.WriteLine($"[FB GraphQL] Clicking post: {postUrl}");
                // Find the link in DOM and click it
                var clicked = await page.EvaluateAsync<bool>($@"
                    () => {{
                        const links = Array.from(document.querySelectorAll('a[href]'));
                        const target = links.find(a => a.href && a.href.includes('{postUrl.Replace("'", @"\'")}'));
                        if (target) {{
                            target.scrollIntoView({{block: 'center'}});
                            target.click();
                            return true;
                        }}
                        return false;
                    }}
                ");
                if (!clicked)
                {
                    Console.WriteLine($"[FB GraphQL] Post link not in DOM (may have been replaced): {postUrl}");
                    continue;
                }
                clickedCount++;

                // Wait for dialog response to be captured
                var waitStart = DateTime.UtcNow;
                var beforeDialog = dialogResponses.Count;
                while (DateTime.UtcNow - waitStart < TimeSpan.FromSeconds(8))
                {
                    await page.WaitForTimeoutAsync(500);
                    if (dialogResponses.Count > beforeDialog) break;
                }

                // Close the dialog (press Escape)
                await page.Keyboard.PressAsync("Escape");
                await page.WaitForTimeoutAsync(800);

                // If dialog didn't open via click, navigate directly as fallback
                if (dialogResponses.Count == beforeDialog && clickedCount <= 2)
                {
                    Console.WriteLine($"[FB GraphQL] Dialog didn't open — trying direct navigation");
                    try
                    {
                        await page.GotoAsync(postUrl, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.Load,
                            Timeout = 15000
                        });
                        await page.WaitForTimeoutAsync(3000);
                        await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.Load });
                        await page.WaitForTimeoutAsync(1500);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FB GraphQL] Error clicking post: {ex.Message}");
            }
        }

        // Cleanup handlers
        page.Response -= OnDialogResponse;
        page.Request -= OnDialogRequest;

        Console.WriteLine($"[FB GraphQL] Clicked {clickedCount} posts, captured {dialogResponses.Count} dialog responses.");

        // Merge dialog responses into the main pool — they have the richest data.
        // Use a HashSet to skip exact duplicates.
        var seenBodies = new HashSet<string>(graphqlResponses);
        foreach (var d in dialogResponses)
        {
            if (seenBodies.Add(d))
                graphqlResponses.Add(d);
        }

        // Save dialog response for debug
        var dialogArray = dialogResponses.ToArray();
        if (dialogArray.Length > 0)
        {
            var debugPath = Path.Combine(
                Path.GetTempPath(),
                $"fb_dialog_debug_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(debugPath, dialogArray[0]);
            Console.WriteLine($"[FB GraphQL] DEBUG: saved first dialog response ({dialogArray[0].Length} chars) to {debugPath}");
        }

        foreach (var json in graphqlResponses.OrderBy(_ => Random.Shared.Next())) // randomise to avoid order bias from duplicate fires
        {
            if (posts.Count >= maxPosts) break;
            if (ct.IsCancellationRequested) break;

            var parsed = ParseFeedResponse(json);
            foreach (var post in parsed)
            {
                if (posts.Count >= maxPosts) break;
                if (string.IsNullOrWhiteSpace(post.PostUrl)) continue;

                // De-dupe by post ID
                var id = ExtractPostId(post.PostUrl);
                if (!seenPostIds.Add(id)) continue;

                if (string.IsNullOrWhiteSpace(post.Text))
                    post.Text = $"[GraphQL] URL: {post.PostUrl}";

                posts.Add(post);
            }
        }

        Console.WriteLine($"[FB GraphQL] Final: {posts.Count} unique posts parsed from {graphqlResponses.Count} responses.");
        return posts;
    }

    /// <summary>
    /// Open a Facebook group page, capture GraphQL feed responses, and return post stubs.
    /// Creates and manages its own Playwright browser/context/page.
    /// Use this when you need a standalone entry point.
    /// </summary>
    public async Task<List<GroupPost>> ScrapeAsync(
        string groupUrl, int maxPosts, ScrapeOptions? options = null, Proxy? proxy = null, CancellationToken ct = default)
    {
        options ??= new ScrapeOptions();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            PlaywrightScrapeHelper.SocialLaunch(options, proxy));

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        await FacebookSessionHelper.LoadCookiesAsync(context);
        var page = await context.NewPageAsync();

        Console.WriteLine("[FB GraphQL] Navigating to: " + groupUrl);
        // NOTE: FB is an SPA — networkidle never fires (websocket/heartbeat constantly).
        // Use "load" + extra wait so React renders and fires initial GraphQL queries.
        await page.GotoAsync(groupUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 30000
        });
        await page.WaitForTimeoutAsync(options.FastDemoMode ? 4000 : 5000);

        await DismissCommonOverlays(page);

        var posts = await ScrapeFeedAsync(page, maxPosts, options, ct);

        await browser.CloseAsync();
        return posts;
    }

    // ── Private: Response filtering ────────────────────────────────────────────

    private static readonly HashSet<string> NoisyEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        // Endpoints that fire constantly but never contain useful post data
        "VideoPlayerHoverOverTagRootQuery",
        "StoriesTrayRootQuery",
        "CometNotificationsRootQuery",
        "CometUFIReactionsCountQuery",
        "CometUFIShareCountQuery",
        "CometUFIVisibilityToggleMutation",
        "BuddySyncDeviceInfoQuery",
        "DTSGInitData",
        "LiveLocationCountQuery",
        "EncryptedSliderImageQuery",
    };

    private static bool IsNoisyEndpoint(string url)
    {
        foreach (var noisy in NoisyEndpoints)
        {
            if (url.Contains(noisy, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool ContainsFeedData(string json)
    {
        // Expanded heuristic: keep any GraphQL response that contains post-like fields.
        // We filter at parse time (TryGetFeedEdges), so here we just skip obvious non-feed responses.
        if (json.Length < 500) return false;
        if (json.Contains("\"__typename\":\"Story\"", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("\"__typename\":\"XFBStory\"", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("\"story_fbid\"", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("\"actors\"", StringComparison.OrdinalIgnoreCase)
            && json.Contains("\"message\"", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("GroupFeed", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("CometFeedRoot", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("page_list", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("timeline_feed_units", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("main_tab_feed", StringComparison.OrdinalIgnoreCase)) return true;
        if (json.Contains("\"creation_time\"", StringComparison.OrdinalIgnoreCase)
            && json.Contains("\"text\"", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ── Private: JSON parsing ────────────────────────────────────────────────────

    private static List<GroupPost> ParseFeedResponse(string json)
    {
        var posts = new List<GroupPost>();

        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var root = doc.RootElement;

            // Facebook GraphQL nests data under different roots depending on A/B test:
            //   data.node.feedback_context.group.feedback
            //   data.node.page_list.edges[].node
            //   data.viewer.main_tab_feed.timeline_feed_modules
            // We try each known path.
            var edges = TryGetFeedEdges(root);

            Console.WriteLine($"[FB GraphQL] ParseFeed: found {edges.Count} edges via TryGetFeedEdges");
            if (edges.Count == 0)
            {
                // Log top-level keys so we can see the actual schema
                Console.WriteLine("[FB GraphQL] ParseFeed: no edges found. Top-level keys: " +
                    string.Join(", ", root.EnumerateObject().Select(p => p.Name).Take(20)));
            }

            foreach (var edge in edges)
            {
                var post = TryParsePostNode(edge);
                if (post != null)
                    posts.Add(post);
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[FB GraphQL] JSON parse error: {ex.Message}");
        }

        return posts;
    }

    /// <summary>
    /// Walk multiple possible root paths to find feed edges array.
    /// </summary>
    private static List<JsonElement> TryGetFeedEdges(JsonElement root)
    {
        var edges = new List<JsonElement>();

        // Try: data.node.page_list.edges[]
        if (TryPath(root, out var e1, "data", "node", "page_list", "edges"))
            edges.AddRange(e1.EnumerateArray());

        // Try: data.node.feedback_context.group.feedback
        if (TryPath(root, out var e2, "data", "node", "feedback_context", "group", "feedback", "top_level_comments", "countable_items"))
            edges.AddRange(e2.EnumerateArray());

        // Try: data.viewer.main_tab_feed.timeline_feed_modules.edges[]
        if (TryPath(root, out var e3, "data", "viewer", "main_tab_feed", "edges"))
            edges.AddRange(e3.EnumerateArray());

        // Try: data.node.shareable_posts.edges[]
        if (TryPath(root, out var e4, "data", "node", "shareable_posts", "edges"))
            edges.AddRange(e4.EnumerateArray());

        // Try: data.node.group_feed.edges[]  (old group format)
        if (TryPath(root, out var e5, "data", "node", "group_feed", "edges"))
            edges.AddRange(e5.EnumerateArray());

        // Try: data.node.feedback.top_level_comments.countable_items[]
        if (TryPath(root, out var e6, "data", "node", "feedback", "top_level_comments", "countable_items"))
            edges.AddRange(e6.EnumerateArray());

        // Try: data.node.comet_sections.content.story (single-post detail)
        // From CometSinglePostDialogContentQuery response schema.
        if (TryPath(root, out var e7, "data", "node", "comet_sections", "content", "story"))
        {
            var single = e7;
            if (single.ValueKind == JsonValueKind.Object)
                edges.Add(single);
        }

        // Try: data.node_v2 (newer schema from POST responses)
        if (TryPath(root, out var e8, "data", "node_v2"))
        {
            var v2 = e8;
            if (v2.ValueKind == JsonValueKind.Object)
                edges.Add(v2);
        }

        // Try: data.node.story (some single-post responses put story directly under node)
        if (TryPath(root, out var e9, "data", "node", "story"))
        {
            var story = e9;
            if (story.ValueKind == JsonValueKind.Object)
                edges.Add(story);
        }

        // Fallback: scan entire JSON for "edges" arrays that look like posts
        if (edges.Count == 0)
            edges.AddRange(ScanForEdges(root));

        return edges;
    }

    /// <summary>
    /// Attempt to recover a valid JSON document when the original ended prematurely
    /// by stripping any trailing content past the last balanced position and
    /// appending missing closing braces. This handles chunked-transfer races where
    /// Playwright fires Response event before the final byte is fully delivered.
    /// Returns null when recovery is not safe.
    /// </summary>
    private static string? TryRecoverTruncatedJson(string body)
    {
        // Walk the string tracking depth of { and [ (ignoring those inside strings)
        int depth = 0;
        bool inString = false;
        bool escape = false;
        int lastBalanced = -1;

        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (escape) { escape = false; continue; }
            if (c == '\\') { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{' || c == '[') depth++;
            else if (c == '}' || c == ']')
            {
                depth--;
                if (depth >= 0) lastBalanced = i;
            }
        }

        // depth > 0 means we never closed everything → truncated
        if (depth <= 0) return null;

        // Truncate at lastBalanced+1 to get a balanced prefix
        // (or just cut at the first incomplete JSON value after lastBalanced)
        var prefix = body[..Math.Min(lastBalanced + 1, body.Length)];

        // Append missing closing braces
        var sb = new System.Text.StringBuilder(prefix);
        for (int i = 0; i < depth; i++) sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Recursively scan for arrays named "edges" that contain post-like nodes.
    /// Used as fallback when the fixed paths above don't match.
    /// </summary>
    private static List<JsonElement> ScanForEdges(JsonElement element, int depth = 0)
    {
        var results = new List<JsonElement>();
        if (depth > 6) return results; // limit recursion

        if (element.ValueKind == JsonValueKind.Array)
        {
            // Check if this array looks like a feed edge list
            var first = element.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object
                && first.TryGetProperty("node", out _)
                && (first.TryGetProperty("cursor", out _) || first.TryGetProperty("creation_time", out _)
                    || first.TryGetProperty("post", out _)))
            {
                results.Add(element);
            }
            return results;
        }

        if (element.ValueKind != JsonValueKind.Object) return results;

        foreach (var prop in element.EnumerateObject())
        {
            results.AddRange(ScanForEdges(prop.Value, depth + 1));
        }

        return results;
    }

    /// <summary>
    /// Parse a single feed edge node into a GroupPost stub.
    /// Handles the fact that Facebook nests the actual post under various intermediate keys.
    /// </summary>
    private static GroupPost? TryParsePostNode(JsonElement edge)
    {
        try
        {
            // Navigate into the "node" wrapper (most edges have it)
            JsonElement node = default;
            if (edge.TryGetProperty("node", out var n))
                node = n;
            else if (edge.TryGetProperty("cursor", out _))
                node = edge; // some edges don't have a "node" wrapper — the edge itself is the post
            else
                node = edge;

            // Some edges wrap another "post" object
            if (node.TryGetProperty("post", out var postProp))
                node = postProp;

            // Re-navigate after unwrapping
            return ExtractPostFromNode(node);
        }
        catch
        {
            return null;
        }
    }

    private static GroupPost? ExtractPostFromNode(JsonElement node)
    {
        var post = new GroupPost();

        // ── post_id ────────────────────────────────────────────────────────────
        string? postId = null;
        if (node.TryGetProperty("post_id", out var pidProp))
            postId = pidProp.GetString();
        if (string.IsNullOrEmpty(postId) && node.TryGetProperty("id", out var idProp))
            postId = idProp.GetString();
        if (string.IsNullOrEmpty(postId) && node.TryGetProperty(" Feedback ", out _))
        {
            // Nested under feedback — dig one level deeper
            if (node.TryGetProperty("target", out var target))
            {
                if (target.TryGetProperty("id", out var tid))
                    postId = tid.GetString();
            }
        }

        // ── text / message ────────────────────────────────────────────────────
        string? text = null;
        if (TryGetText(node, "message", out text)) { }
        else if (TryGetText(node, "body", out text)) { }
        else if (TryGetText(node, "text", out text)) { }
        else if (TryGetText(node, "content", out text)) { }
        // Schema from CometSinglePostDialogContentQuery:
        // comet_sections.content.story.comet_sections.message.rich_message[] (array of text blocks)
        else if (TryPath(node, out var richMsgEl,
                "comet_sections", "content", "story", "comet_sections", "message", "rich_message")
                 && richMsgEl.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var block in richMsgEl.EnumerateArray())
            {
                if (block.TryGetProperty("text", out var blockText))
                    sb.AppendLine(blockText.GetString());
            }
            text = sb.ToString().Trim();
        }
        // Newer node_v2 schema: feedback.message
        else if (TryPath(node, out var fbMsgEl, "feedback", "message", "text")
                 && fbMsgEl.ValueKind == JsonValueKind.String)
        {
            text = fbMsgEl.GetString();
        }
        else if (node.TryGetProperty("attachments", out var attachments) && attachments.ValueKind == JsonValueKind.Array)
        {
            foreach (var att in attachments.EnumerateArray())
            {
                if (att.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
                {
                    text = desc.GetString();
                    break;
                }
            }
        }
        post.Text = text ?? "";

        // ── author ────────────────────────────────────────────────────────────
        if (node.TryGetProperty("actors", out var actors) && actors.ValueKind == JsonValueKind.Array)
        {
            var first = actors.EnumerateArray().FirstOrDefault();
            if (first.TryGetProperty("name", out var name))
                post.Author = name.GetString() ?? "";
            else if (first.TryGetProperty("id", out var actorId))
                post.Author = actorId.GetString() ?? "";
        }
        else if (node.TryGetProperty("author", out var author))
        {
            if (author.ValueKind == JsonValueKind.String)
                post.Author = author.GetString() ?? "";
            else if (author.TryGetProperty("name", out var authorName))
                post.Author = authorName.GetString() ?? "";
        }
        else if (node.TryGetProperty("username", out var uname))
        {
            post.Author = uname.GetString() ?? "";
        }

        // ── post URL / permalink ───────────────────────────────────────────────
        if (node.TryGetProperty("url", out var urlProp))
            post.PostUrl = NormalizeFacebookUrl(urlProp.GetString() ?? "");
        else if (node.TryGetProperty("permalink_url", out var permalink))
            post.PostUrl = NormalizeFacebookUrl(permalink.GetString() ?? "");
        else if (node.TryGetProperty("share_url", out var shareUrl))
            post.PostUrl = NormalizeFacebookUrl(shareUrl.GetString() ?? "");
        else if (!string.IsNullOrEmpty(postId))
            post.PostUrl = $"https://www.facebook.com/permalink.php?story_fbid={postId}";

        // ── creation_time / posted_at ──────────────────────────────────────────
        DateTime? postedAt = null;
        if (node.TryGetProperty("creation_time", out var ctProp))
        {
            if (ctProp.ValueKind == JsonValueKind.Number)
            {
                var unix = ctProp.GetInt64();
                postedAt = unix > 1_000_000_000_000 // milliseconds
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unix).DateTime
                    : DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
            }
            else if (ctProp.ValueKind == JsonValueKind.String
                     && long.TryParse(ctProp.GetString(), out var unixStr))
            {
                postedAt = unixStr > 1_000_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixStr).DateTime
                    : DateTimeOffset.FromUnixTimeSeconds(unixStr).DateTime;
            }
        }
        else if (node.TryGetProperty("publish_time", out var pt) && pt.ValueKind == JsonValueKind.Number)
        {
            var unix = pt.GetInt64();
            postedAt = unix > 1_000_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix).DateTime
                : DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
        }
        post.PostedAt = postedAt;

        // ── comment count (optional) ──────────────────────────────────────────
        if (node.TryGetProperty("comment_count", out var cc) && cc.ValueKind == JsonValueKind.Number)
        {
            // we could store this but GroupPost doesn't have a field for it yet
        }

        return post;
    }

    /// <summary>
    /// Navigate a deeply-nested JSON element by a path of property names.
    /// </summary>
    private static bool TryPath(JsonElement root, out JsonElement result, params string[] path)
    {
        var current = root;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                result = default;
                return false;
            }
            if (!current.TryGetProperty(key, out var next))
            {
                result = default;
                return false;
            }
            current = next;
        }
        result = current;
        return true;
    }

    private static bool TryGetText(JsonElement element, string property, out string? text)
    {
        text = null;
        if (!element.TryGetProperty(property, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.String)
        {
            text = prop.GetString();
            return !string.IsNullOrWhiteSpace(text);
        }

        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("text", out var innerText))
        {
            text = innerText.GetString();
            return !string.IsNullOrWhiteSpace(text);
        }

        return false;
    }

    private static string ExtractPostId(string postUrl)
    {
        // Extract identifier from permalink URL
        var uri = new Uri(postUrl.StartsWith("http") ? postUrl : "https://facebook.com" + postUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        if (query["story_fbid"] is string sfbid && !string.IsNullOrEmpty(sfbid))
            return sfbid;
        if (query["fbid"] is string fbid && !string.IsNullOrEmpty(fbid))
            return fbid;
        if (query["id"] is string id && !string.IsNullOrEmpty(id))
            return id;

        // Fall back to the path segment
        var segs = uri.Segments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        return segs.Length > 0 ? segs[^1].TrimEnd('/') : postUrl;
    }

    private static string NormalizeFacebookUrl(string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return "";

        var url = href.StartsWith('/') ? "https://www.facebook.com" + href : href;
        var q = url.IndexOf('?');
        if (q < 0) return url;

        var query = url[(q + 1)..];
        var keep = query.Split('&')
            .Where(p => p.StartsWith("id=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("story_fbid=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("fbid=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var baseUrl = url[..q];
        return keep.Count > 0 ? baseUrl + "?" + string.Join('&', keep) : baseUrl;
    }

    private static Task DismissCommonOverlays(IPage page)
    {
        var labels = new[] { "Close", "Đóng", "Allow all cookies", "Cho phép tất cả cookie", "Từ chối cookie không cần thiết" };
        return Task.Run(async () =>
        {
            foreach (var label in labels)
            {
                try
                {
                    var btn = page.GetByRole(AriaRole.Button, new() { Name = label });
                    if (await btn.CountAsync() > 0)
                    {
                        await btn.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                        await page.WaitForTimeoutAsync(400);
                    }
                }
                catch { }
            }
        });
    }
}
