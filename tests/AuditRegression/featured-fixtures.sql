-- ONLY run against the isolated database created by AuditRegression.
BEGIN;
INSERT INTO users (email,full_name,role,is_active,created_at)
SELECT 'featured-ui-' || n || '@example.test', 'Featured Doctor ' || n, 'DOCTOR', true, now()
FROM generate_series(1,8) n ON CONFLICT (email) DO NOTHING;
INSERT INTO doctor_details (user_id,specialty_id,facility_id,verification_status,rating_average,review_count,price)
SELECT u.user_id, d.specialty_id, d.facility_id, 'APPROVED',
 CASE WHEN n=8 THEN NULL ELSE 5.0 - n*0.1 END, CASE WHEN n=8 THEN 0 ELSE 10 END, 100000
FROM generate_series(1,8) n
JOIN users u ON u.email='featured-ui-' || n || '@example.test'
CROSS JOIN (SELECT specialty_id,facility_id FROM doctor_details ORDER BY doctor_id LIMIT 1) d
ON CONFLICT (user_id) DO NOTHING;
COMMIT;
