CREATE TABLE `TlsEntity` (
  `hostname` varchar(256) NOT NULL,
  `state` json NOT NULL,
  PRIMARY KEY (`hostname`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

GRANT SELECT, INSERT, UPDATE, DELETE ON `TlsEntity` TO '{env}-tls-entity' IDENTIFIED BY '{password}';

GRANT SELECT ON `TlsEntity` TO '{env}-mx-api' IDENTIFIED BY '{password}';