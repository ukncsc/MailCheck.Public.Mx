CREATE TABLE `Domain` (
  `domain` varchar(256) NOT NULL,
  `mxState` int(11) NOT NULL,
  `lastUpdated` datetime,
  `error` json DEFAULT NULL,
  PRIMARY KEY (`domain`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

GRANT SELECT, INSERT, UPDATE, DELETE ON `Domain` TO '{env}-mx-entity' IDENTIFIED BY '{password}';

GRANT SELECT ON `Domain` TO '{env}-mx-api' IDENTIFIED BY '{password}';