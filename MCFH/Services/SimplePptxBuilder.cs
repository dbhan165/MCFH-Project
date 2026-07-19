using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace MCFH.Services;

public static class SimplePptxBuilder
{
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Build(string title, IReadOnlyList<PptxSlide> slides)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes(slides.Count));
            Write(zip, "_rels/.rels", RootRels());
            Write(zip, "docProps/core.xml", CoreProps(title));
            Write(zip, "docProps/app.xml", AppProps(slides.Count + 1));
            Write(zip, "ppt/presentation.xml", Presentation(slides.Count + 1));
            Write(zip, "ppt/_rels/presentation.xml.rels", PresentationRels(slides.Count + 1));
            Write(zip, "ppt/theme/theme1.xml", Theme());
            Write(zip, "ppt/slideMasters/slideMaster1.xml", SlideMaster());
            Write(zip, "ppt/slideMasters/_rels/slideMaster1.xml.rels", SlideMasterRels());
            Write(zip, "ppt/slideLayouts/slideLayout1.xml", SlideLayout());
            Write(zip, "ppt/slideLayouts/_rels/slideLayout1.xml.rels", SlideLayoutRels());

            Write(zip, "ppt/slides/slide1.xml", TitleSlide(title));
            Write(zip, "ppt/slides/_rels/slide1.xml.rels", SlideRels(1));

            for (var i = 0; i < slides.Count; i++)
            {
                var slideNo = i + 2;
                var slideContent = slides[i].ChartData.Count > 0 ? BarChartSlide(slides[i]) : BulletSlide(slides[i]);
                Write(zip, $"ppt/slides/slide{slideNo}.xml", slideContent);
                Write(zip, $"ppt/slides/_rels/slide{slideNo}.xml.rels", SlideRels(slideNo));
            }
        }

        return ms.ToArray();
    }

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypes(int slideCount) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>
          <Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>
          <Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
          <Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
          <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
          <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
        {string.Concat(Enumerable.Range(1, slideCount + 1).Select(i => $"""
          <Override PartName="/ppt/slides/slide{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>
        """))}
        </Types>
        """;

    private static string RootRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
        </Relationships>
        """;

    private static string CoreProps(string title) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <dc:title>{EscapeXml(title)}</dc:title>
          <dc:creator>MCFH Platform</dc:creator>
          <cp:lastModifiedBy>MCFH Platform</cp:lastModifiedBy>
        </cp:coreProperties>
        """;

    private static string AppProps(int slideCount) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
          <Application>MCFH</Application>
          <Slides>{slideCount}</Slides>
        </Properties>
        """;

    private static string Presentation(int slideCount)
    {
        var slideIds = string.Concat(Enumerable.Range(1, slideCount).Select(i =>
            $"""<p:sldId id="{255 + i}" r:id="rId{i}"/>"""));
        return $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
          <p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId{slideCount + 1}"/></p:sldMasterIdLst>
          <p:sldIdLst>{slideIds}</p:sldIdLst>
          <p:sldSz cx="9144000" cy="6858000" type="screen4x3"/>
          <p:notesSz cx="6858000" cy="9144000"/>
        </p:presentation>
        """;
    }

    private static string PresentationRels(int slideCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.AppendLine("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (var i = 1; i <= slideCount; i++)
            sb.AppendLine($"""  <Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide{i}.xml"/>""");
        sb.AppendLine($"""  <Relationship Id="rId{slideCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>""");
        sb.AppendLine("""</Relationships>""");
        return sb.ToString();
    }

    private static string SlideRels(int slideNo) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
        </Relationships>
        """;

    private static string SlideMasterRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/>
        </Relationships>
        """;

    private static string SlideLayoutRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/>
        </Relationships>
        """;

    private static string Theme() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="MCFH">
          <a:themeElements>
            <a:clrScheme name="MCFH">
              <a:dk1><a:srgbClr val="0F172A"/></a:dk1>
              <a:lt1><a:srgbClr val="FFFFFF"/></a:lt1>
              <a:dk2><a:srgbClr val="151B2B"/></a:dk2>
              <a:lt2><a:srgbClr val="E2E8F0"/></a:lt2>
              <a:accent1><a:srgbClr val="FF7575"/></a:accent1>
              <a:accent2><a:srgbClr val="00B4D8"/></a:accent2>
              <a:accent3><a:srgbClr val="10B981"/></a:accent3>
              <a:accent4><a:srgbClr val="F59E0B"/></a:accent4>
              <a:accent5><a:srgbClr val="8B5CF6"/></a:accent5>
              <a:accent6><a:srgbClr val="64748B"/></a:accent6>
              <a:hlink><a:srgbClr val="00B4D8"/></a:hlink>
              <a:folHlink><a:srgbClr val="FF7575"/></a:folHlink>
            </a:clrScheme>
            <a:fontScheme name="MCFH">
              <a:majorFont><a:latin typeface="Segoe UI"/></a:majorFont>
              <a:minorFont><a:latin typeface="Segoe UI"/></a:minorFont>
            </a:fontScheme>
            <a:fmtScheme name="MCFH"><a:fillStyleLst/><a:lnStyleLst/><a:effectStyleLst/><a:bgFillStyleLst/></a:fmtScheme>
          </a:themeElements>
        </a:theme>
        """;

    private static string SlideMaster() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
          <p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/></p:spTree></p:cSld>
          <p:clrMap bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/>
          <p:sldLayoutIdLst><p:sldLayoutId id="1" r:id="rId1"/></p:sldLayoutIdLst>
        </p:sldMaster>
        """;

    private static string SlideLayout() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank" preserve="1">
          <p:cSld name="Blank"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/></p:spTree></p:cSld>
        </p:sldLayout>
        """;

    private static string TitleSlide(string title)
    {
        var doc = new XDocument(
            new XElement(P + "sld",
                new XAttribute(XNamespace.Xmlns + "a", A),
                new XAttribute(XNamespace.Xmlns + "r", R),
                new XAttribute(XNamespace.Xmlns + "p", P),
                new XElement(P + "cSld",
                    new XElement(P + "spTree",
                        new XElement(P + "nvGrpSpPr",
                            new XElement(P + "cNvPr", new XAttribute("id", "1"), new XAttribute("name", "")),
                            new XElement(P + "cNvGrpSpPr"),
                            new XElement(P + "nvPr")),
                        new XElement(P + "grpSpPr"),
                        TextBox(2, title, 457200, 2286000, 8229600, 914400, 4400, true),
                        TextBox(3, "MCFH Social Listening Report", 457200, 3505200, 8229600, 457200, 2000, false)))));
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" + doc;
    }

    private static string BulletSlide(PptxSlide slide)
    {
        var children = new List<XElement>
        {
            new XElement(P + "nvGrpSpPr",
                new XElement(P + "cNvPr", new XAttribute("id", "1"), new XAttribute("name", "")),
                new XElement(P + "cNvGrpSpPr"),
                new XElement(P + "nvPr")),
            new XElement(P + "grpSpPr"),
            TextBox(2, slide.Heading, 457200, 365760, 8229600, 685800, 3200, true)
        };

        var bulletBody = string.Join("\n", slide.Bullets.Select(b => $"• {b}"));
        children.Add(TextBox(3, bulletBody, 457200, 1371600, 8229600, 4572000, 1800, false));

        var doc = new XDocument(
            new XElement(P + "sld",
                new XAttribute(XNamespace.Xmlns + "a", A),
                new XAttribute(XNamespace.Xmlns + "r", R),
                new XAttribute(XNamespace.Xmlns + "p", P),
                new XElement(P + "cSld", new XElement(P + "spTree", children))));

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" + doc;
    }

    private static string BarChartSlide(PptxSlide slide)
    {
        var children = new List<XElement>
        {
            new XElement(P + "nvGrpSpPr",
                new XElement(P + "cNvPr", new XAttribute("id", "1"), new XAttribute("name", "")),
                new XElement(P + "cNvGrpSpPr"),
                new XElement(P + "nvPr")),
            new XElement(P + "grpSpPr"),
            TextBox(2, slide.Heading, 457200, 365760, 8229600, 685800, 3200, true)
        };

        if (slide.Bullets.Count > 0)
        {
            var bulletBody = string.Join("\n", slide.Bullets.Select(b => $"• {b}"));
            children.Add(TextBox(3, bulletBody, 457200, 1100000, 8229600, 800000, 1600, false));
        }

        long startY = slide.Bullets.Count > 0 ? 2000000 : 1371600;
        long maxW = 6000000;
        long h = 400000;
        long gap = 150000;
        long currentY = startY;
        long labelX = 457200;
        long barX = 2500000;
        
        var maxVal = slide.ChartData.Count > 0 ? slide.ChartData.Max(d => d.Value) : 1;
        if (maxVal <= 0) maxVal = 1;

        int id = 4;
        foreach (var item in slide.ChartData)
        {
            // Label
            children.Add(TextBox(id++, item.Label, labelX, currentY, 1900000, h, 1400, false));
            
            // Bar
            long barW = (long)(maxW * (item.Value / maxVal));
            if (barW < 50000) barW = 50000;
            children.Add(Rectangle(id++, "", barX, currentY, barW, h, item.ColorHex, 1400, "FFFFFF"));
            
            // Value Label (placed after bar)
            children.Add(TextBox(id++, item.ValueLabel, barX + barW + 100000, currentY, 1500000, h, 1400, true));

            currentY += h + gap;
        }

        var doc = new XDocument(
            new XElement(P + "sld",
                new XAttribute(XNamespace.Xmlns + "a", A),
                new XAttribute(XNamespace.Xmlns + "r", R),
                new XAttribute(XNamespace.Xmlns + "p", P),
                new XElement(P + "cSld", new XElement(P + "spTree", children))));

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" + doc;
    }

    private static XElement TextBox(int id, string text, long x, long y, long cx, long cy, int fontSize, bool bold)
    {
        var runs = text.Split('\n').SelectMany((line, index) =>
        {
            var elements = new List<XElement>
            {
                new XElement(A + "r",
                    new XElement(A + "rPr",
                        new XAttribute("lang", "vi-VN"),
                        new XAttribute("sz", fontSize),
                        bold ? new XElement(A + "b") : null),
                    new XElement(A + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), line))
            };
            if (index < text.Split('\n').Length - 1)
                elements.Add(new XElement(A + "br"));
            return elements;
        });

        return new XElement(P + "sp",
            new XElement(P + "nvSpPr",
                new XElement(P + "cNvPr", new XAttribute("id", id), new XAttribute("name", $"TextBox {id}")),
                new XElement(P + "cNvSpPr", new XAttribute("txBox", "1")),
                new XElement(P + "nvPr")),
            new XElement(P + "spPr",
                new XElement(A + "xfrm",
                    new XElement(A + "off", new XAttribute("x", x), new XAttribute("y", y)),
                    new XElement(A + "ext", new XAttribute("cx", cx), new XAttribute("cy", cy))),
                new XElement(A + "prstGeom", new XAttribute("prst", "rect"),
                    new XElement(A + "avLst"))),
            new XElement(P + "txBody",
                new XElement(A + "bodyPr", new XAttribute("wrap", "square")),
                new XElement(A + "lstStyle"),
                new XElement(A + "p", runs)));
    }

    private static XElement Rectangle(int id, string text, long x, long y, long cx, long cy, string colorHex, int fontSize, string textColorHex)
    {
        var runs = text.Split('\n').SelectMany((line, index) =>
        {
            var elements = new List<XElement>
            {
                new XElement(A + "r",
                    new XElement(A + "rPr",
                        new XAttribute("lang", "vi-VN"),
                        new XAttribute("sz", fontSize),
                        new XElement(A + "solidFill", new XElement(A + "srgbClr", new XAttribute("val", textColorHex)))),
                    new XElement(A + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), line))
            };
            if (index < text.Split('\n').Length - 1)
                elements.Add(new XElement(A + "br"));
            return elements;
        });

        return new XElement(P + "sp",
            new XElement(P + "nvSpPr",
                new XElement(P + "cNvPr", new XAttribute("id", id), new XAttribute("name", $"Rect {id}")),
                new XElement(P + "cNvSpPr"),
                new XElement(P + "nvPr")),
            new XElement(P + "spPr",
                new XElement(A + "xfrm",
                    new XElement(A + "off", new XAttribute("x", x), new XAttribute("y", y)),
                    new XElement(A + "ext", new XAttribute("cx", cx), new XAttribute("cy", cy))),
                new XElement(A + "prstGeom", new XAttribute("prst", "rect"),
                    new XElement(A + "avLst")),
                new XElement(A + "solidFill", new XElement(A + "srgbClr", new XAttribute("val", colorHex))),
                new XElement(A + "ln", new XElement(A + "noFill"))),
            new XElement(P + "txBody",
                new XElement(A + "bodyPr", new XAttribute("wrap", "square"), new XAttribute("lIns", "91440"), new XAttribute("tIns", "45720"), new XAttribute("rIns", "91440"), new XAttribute("bIns", "45720"), new XAttribute("anchor", "ctr")),
                new XElement(A + "lstStyle"),
                new XElement(A + "p",
                    new XElement(A + "pPr", new XAttribute("algn", "l")),
                    runs)));
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

public sealed class PptxBarChartItem
{
    public string Label { get; init; } = "";
    public double Value { get; init; }
    public string ColorHex { get; init; } = "FF7575";
    public string ValueLabel { get; init; } = "";
}

public sealed class PptxSlide
{
    public string Heading { get; init; } = "";
    public IReadOnlyList<string> Bullets { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PptxBarChartItem> ChartData { get; init; } = Array.Empty<PptxBarChartItem>();
}
