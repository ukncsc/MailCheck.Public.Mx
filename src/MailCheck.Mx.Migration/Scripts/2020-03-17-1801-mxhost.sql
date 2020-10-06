CREATE TABLE `MxHost` (
  `hostname` varchar(256) NOT NULL,
  `hostMxRecord` json NOT NULL,
  `lastUpdated` datetime,
  PRIMARY KEY (`hostname`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

GRANT SELECT, INSERT, UPDATE, DELETE ON `MxHost` TO '{env}-mx-entity' IDENTIFIED BY '{password}';

GRANT SELECT ON `MxHost` TO '{env}-mx-api' IDENTIFIED BY '{password}';