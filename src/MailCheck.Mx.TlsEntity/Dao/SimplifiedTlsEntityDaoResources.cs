namespace MailCheck.Mx.TlsEntity.Dao
{
    public class SimplifiedTlsEntityDaoResources
    {
        public const string GetIpAddressesForHost = @"SELECT hostname, ipAddress FROM mx.SimplifiedTlsEntity WHERE hostname = @hostname;";

        public const string GetHostnameForIp = @"SELECT hostname, ipAddress FROM mx.SimplifiedTlsEntity WHERE ipAddress = @ipAddress AND hostname != '*';";

        public const string SaveIpAddressesHostnameAssociciation = @"INSERT IGNORE INTO mx.SimplifiedTlsEntity ( hostname, ipAddress ) VALUES ( @hostname, @ipAddress );";

        public const string DeleteIpAddressHostnameAssociations = @"DELETE FROM mx.SimplifiedTlsEntity WHERE ipAddress = @ipAddress AND hostname = @hostname;";

        public const string GetStateForIpAndHost = @"
SELECT hostname, ipAddress, json
FROM mx.SimplifiedTlsEntity 
WHERE ipAddress = @ipAddress AND hostname = @hostname;";

        public const string SaveStateForIpAndHost = @"
INSERT INTO mx.SimplifiedTlsEntity ( hostname, ipAddress, json ) 
VALUES ( @hostname, @ipAddress, @state)
ON DUPLICATE KEY UPDATE json = @state;
";

        public const string FindEntitiesByIp = @"
SELECT hostname, ipAddress, json
FROM mx.SimplifiedTlsEntity 
WHERE ipAddress = @ipAddress;";

        public const string FindRelatedEntitiesByIp = @"
SELECT hostname, ipAddress, json
FROM mx.SimplifiedTlsEntity
WHERE hostname IN (
	SELECT hostname
    FROM mx.SimplifiedTlsEntity
    WHERE ipAddress = @ipAddress
    AND hostname != '*'
)
UNION
SELECT hostname, ipAddress, json
FROM mx.SimplifiedTlsEntity 
WHERE ipAddress IN (SELECT DISTINCT ip.ipAddress
FROM mx.SimplifiedTlsEntity ip
JOIN mx.SimplifiedTlsEntity hosts
ON ip.hostname = hosts.hostname
AND hosts.ipAddress = @ipAddress
AND hosts.hostname != '*'
)
AND hostname = '*'
UNION
SELECT distinct hostname, ipAddress, json
FROM mx.SimplifiedTlsEntity 
WHERE ipAddress = @ipAddress;";

        public const string GetHostnamesWithDomainsForIp = @"
SELECT rec.hostname, rec.domain 
FROM mx.SimplifiedTlsEntity host 
INNER JOIN mx.MxRecord rec 
    ON host.hostname = rec.hostname 
WHERE host.ipAddress = @ipAddress";

        public const string GetAdvisoryStatusesForAffectedDomainsByMxHostIp = @"
SELECT 
    mr1.domain, 
    mr1.hostname,
    IF(ipEntity2.json is null, null, 
    JSON_MERGE(
       COALESCE(JSON_EXTRACT(hostEntity2.json, '$.tlsAdvisories[*].messageType', '$.certAdvisories[*].messageType'), JSON_ARRAY()),
       COALESCE(JSON_EXTRACT(ipEntity2.json, '$.tlsAdvisories[*].messageType', '$.certAdvisories[*].messageType'), JSON_ARRAY()))
    ) as statuses
FROM mx.MxRecord mr1
JOIN mx.MxRecord mr2
  ON mr1.domain = mr2.domain
JOIN mx.SimplifiedTlsEntity hostEntity 
  ON mr2.hostname = hostEntity.hostname
  AND hostEntity.ipAddress = @ipAddress
JOIN mx.SimplifiedTlsEntity hostEntity2
  ON mr1.hostname = hostEntity2.hostname
LEFT JOIN mx.SimplifiedTlsEntity ipEntity2
  ON hostEntity2.ipAddress = ipEntity2.ipAddress
  AND ipEntity2.hostname = '*'";
    }
}
