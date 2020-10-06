GRANT SELECT, INSERT, UPDATE, DELETE ON `Domain` TO '{env}-mx-entity' IDENTIFIED BY '{password}';
GRANT SELECT, INSERT, UPDATE, DELETE ON `MxHost` TO '{env}-mx-entity' IDENTIFIED BY '{password}';
GRANT SELECT, INSERT, UPDATE, DELETE ON `MxRecord` TO '{env}-mx-entity' IDENTIFIED BY '{password}';

GRANT SELECT, INSERT, UPDATE, DELETE ON `TlsEntity` TO '{env}-tls-entity' IDENTIFIED BY '{password}';
GRANT SELECT ON `MxRecord` TO '{env}-tls-entity' IDENTIFIED BY '{password}';

GRANT SELECT ON `TlsEntity` TO '{env}-mx-api' IDENTIFIED BY '{password}';
GRANT SELECT ON `MxRecord` TO '{env}-mx-api' IDENTIFIED BY '{password}';
GRANT SELECT ON `MxHost` TO '{env}-mx-api' IDENTIFIED BY '{password}';
GRANT SELECT ON `Domain` TO '{env}-mx-api' IDENTIFIED BY '{password}';

GRANT SELECT ON `MxHost` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `Domain` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `MxRecord` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `TlsEntity` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT INTO S3 ON *.* TO '{env}_reports' IDENTIFIED BY '{password}';