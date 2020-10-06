GRANT SELECT ON `MxHost` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `Domain` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `MxRecord` TO '{env}_reports' IDENTIFIED BY '{password}';
GRANT SELECT ON `TlsEntity` TO '{env}_reports' IDENTIFIED BY '{password}';