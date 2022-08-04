CREATE TABLE `HostnameIpAddress` (
  `hostname` varchar(256) NOT NULL,
  `ipAddress` varchar(256) NOT NULL,
  `json` json,
  CONSTRAINT HOSTNAME_IPADDRESS PRIMARY KEY (hostname, ipAddress)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE INDEX INDEX_IPADDRESS_HOSTNAME ON `HostnameIpAddress` (ipAddress, hostname);