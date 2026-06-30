-- Seed 10 proxy Webshare vào SYSTEM_PROXIES
SET NOCOUNT ON;

INSERT INTO SYSTEM_PROXIES (ip_address, port, auth_user, auth_pass, status, fail_count, last_used_at)
VALUES
    ('31.59.20.176',  6754, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('31.56.127.193', 7684, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('45.38.107.97',  6014, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('38.154.203.95', 5863, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('198.105.121.200', 6462, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('64.137.96.74',  6641, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('198.23.243.226', 6361, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('38.154.185.97', 6370, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('142.111.67.146', 5611, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL),
    ('191.96.254.138', 6185, 'icorqcdt', 'wn92syqzt1p1', 'active', 0, NULL);

SELECT proxy_id, ip_address, port, auth_user, status, fail_count FROM SYSTEM_PROXIES ORDER BY proxy_id;
