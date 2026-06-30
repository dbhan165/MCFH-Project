-- Giữ lại PROJECTS: 1, 4, 16 — xóa các project còn lại và dữ liệu liên quan.
-- Chạy: sqlcmd -S localhost -d MCFH_DB -E -i CleanupProjectsKeepIds.sql

SET NOCOUNT ON;

DECLARE @Keep TABLE (id INT PRIMARY KEY);
INSERT INTO @Keep (id) VALUES (1), (4), (16);

BEGIN TRANSACTION;

BEGIN TRY
    -- Feedback / AI
    DELETE mt
    FROM MENTION_TAGS mt
    INNER JOIN SCRAPED_FEEDBACKS f ON f.feedback_id = mt.feedback_id
    WHERE f.project_id NOT IN (SELECT id FROM @Keep);

    DELETE fa
    FROM FEEDBACK_ASPECTS fa
    INNER JOIN AI_ANALYSIS a ON a.analysis_id = fa.analysis_id
    INNER JOIN SCRAPED_FEEDBACKS f ON f.feedback_id = a.feedback_id
    WHERE f.project_id NOT IN (SELECT id FROM @Keep);

    DELETE a
    FROM AI_ANALYSIS a
    INNER JOIN SCRAPED_FEEDBACKS f ON f.feedback_id = a.feedback_id
    WHERE f.project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM SCRAPED_FEEDBACKS
    WHERE project_id NOT IN (SELECT id FROM @Keep);

    -- Jobs & orders
    DELETE FROM SCRAPING_JOBS
    WHERE project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM SCRAPE_ORDERS
    WHERE project_id NOT IN (SELECT id FROM @Keep);

    -- Analytics / misc per project
    DELETE FROM NOTIFICATIONS
    WHERE project_id IS NOT NULL AND project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM NSR_SNAPSHOTS WHERE project_id NOT IN (SELECT id FROM @Keep);
    DELETE FROM INFLUENCERS WHERE project_id NOT IN (SELECT id FROM @Keep);
    DELETE FROM MUTED_ENTITIES WHERE project_id NOT IN (SELECT id FROM @Keep);
    DELETE FROM IMPORT_FILES WHERE project_id NOT IN (SELECT id FROM @Keep);
    DELETE FROM SAVED_FILTERS WHERE project_id NOT IN (SELECT id FROM @Keep);

    DELETE mt
    FROM MENTION_TAGS mt
    INNER JOIN TAGS t ON t.tag_id = mt.tag_id
    WHERE t.project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM TAGS WHERE project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM DATA_SOURCES
    WHERE project_id NOT IN (SELECT id FROM @Keep);

    DELETE FROM PROJECTS
    WHERE project_id NOT IN (SELECT id FROM @Keep);

    COMMIT TRANSACTION;
    PRINT 'Cleanup projects: OK — kept 1, 4, 16';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT project_id, name, workspace_id, is_deleted FROM PROJECTS ORDER BY project_id;
SELECT COUNT(*) AS data_sources FROM DATA_SOURCES;
