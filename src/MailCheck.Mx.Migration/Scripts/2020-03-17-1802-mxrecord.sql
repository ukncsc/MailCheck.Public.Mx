CREATE TABLE `MxRecord` (
  `domain` varchar(256) NOT NULL,
  `hostname` varchar(256) NOT NULL,
  `preference` int(11) NOT NULL,
  PRIMARY KEY (`domain`,`hostname`),
  KEY `hostname` (`hostname`),
  CONSTRAINT `MxRecord_ibfk_1` FOREIGN KEY (`domain`) REFERENCES `Domain` (`domain`) ON DELETE CASCADE,
  CONSTRAINT `MxRecord_ibfk_2` FOREIGN KEY (`hostname`) REFERENCES `MxHost` (`hostname`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

GRANT SELECT, INSERT, UPDATE, DELETE ON `MxRecord` TO '{env}-mx-entity' IDENTIFIED BY '{password}';

GRANT SELECT ON `MxRecord` TO '{env}-mx-api' IDENTIFIED BY '{password}';