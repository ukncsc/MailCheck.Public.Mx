﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MailCheck.Mx.Entity.Dao {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class MxStateDaoResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MxStateDaoResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MailCheck.Mx.Entity.Dao.MxStateDaoResources", typeof(MxStateDaoResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///DELETE FROM `mx`.`Domain`
        ///WHERE domain = @domain;
        ///    .
        /// </summary>
        public static string DeleteDomain {
            get {
                return ResourceManager.GetString("DeleteDomain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///DELETE FROM `mx`.`MxHost`
        ///WHERE hostname IN (.
        /// </summary>
        public static string DeleteHosts {
            get {
                return ResourceManager.GetString("DeleteHosts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @a{0}.
        /// </summary>
        public static string DeleteHostsValueFormatString {
            get {
                return ResourceManager.GetString("DeleteHostsValueFormatString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///DELETE FROM `mx`.`MxRecord`
        ///WHERE domain = @domain;
        ///    .
        /// </summary>
        public static string DeleteMxRecord {
            get {
                return ResourceManager.GetString("DeleteMxRecord", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///SELECT target.hostname
        ///FROM `mx`.`MxRecord` target
        ///WHERE target.domain = @domain
        ///AND NOT EXISTS (SELECT * FROM `mx`.`MxRecord` others
        ///  WHERE target.hostname = others.hostname
        ///  AND others.domain != @domain)
        ///    .
        /// </summary>
        public static string GetHostsUniqueToDomain {
            get {
                return ResourceManager.GetString("GetHostsUniqueToDomain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///SELECT d.domain, d.mxState, h.hostMxRecord, d.lastUpdated, d.error 
        ///FROM mx.MxRecord
        ///JOIN MxHost h
        ///ON h.hostname = MxRecord.hostname
        ///RIGHT JOIN Domain d
        ///ON d.domain = MxRecord.domain
        ///WHERE d.domain = @domain;
        ///    .
        /// </summary>
        public static string GetMxRecord {
            get {
                return ResourceManager.GetString("GetMxRecord", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///SELECT hostEntity.ipAddress, hostEntity.json hostJson, ipAddressEntity.json ipAddressJson
        ///FROM mx.SimplifiedTlsEntity hostEntity
        ///JOIN mx.SimplifiedTlsEntity ipAddressEntity ON hostEntity.ipAddress = ipAddressEntity.ipAddress AND ipAddressEntity.hostname = &apos;*&apos;
        ///WHERE hostEntity.hostname = @hostname
        ///.
        /// </summary>
        public static string GetSimplifiedStates {
            get {
                return ResourceManager.GetString("GetSimplifiedStates", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///INSERT INTO `mx`.`Domain`
        ///(`domain`,
        ///`mxState`,
        ///`lastUpdated`,
        ///`error`)
        ///VALUES
        ///(@domain,
        ///@mxState,
        ///@lastUpdated,
        ///@error)
        ///ON DUPLICATE KEY UPDATE
        ///`mxState` = @mxState,
        ///`lastUpdated` = COALESCE(@lastUpdated, lastUpdated),
        ///`error` = COALESCE(@error, error);
        ///    .
        /// </summary>
        public static string UpsertDomain {
            get {
                return ResourceManager.GetString("UpsertDomain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///INSERT INTO `mx`.`MxHost`
        ///(hostname,
        ///hostMxRecord,
        ///lastUpdated)
        ///VALUES
        ///{MxHostValues}
        ///ON DUPLICATE KEY UPDATE
        ///hostMxRecord = VALUES(hostMxRecord),
        ///lastUpdated = VALUES(lastUpdated);
        ///    .
        /// </summary>
        public static string UpsertMxHost {
            get {
                return ResourceManager.GetString("UpsertMxHost", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///INSERT INTO `mx`.`MxRecord`
        ///(domain,
        ///hostname,
        ///preference)
        ///VALUES
        ///{MxRecordValues}
        ///ON DUPLICATE KEY UPDATE
        ///preference = VALUES(preference);
        ///    .
        /// </summary>
        public static string UpsertMxRecord {
            get {
                return ResourceManager.GetString("UpsertMxRecord", resourceCulture);
            }
        }
    }
}
