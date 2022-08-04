﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MailCheck.Mx.Api.Dao {
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
    public class MxApiDaoResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MxApiDaoResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MailCheck.Mx.Api.Dao.MxApiDaoResources", typeof(MxApiDaoResources).Assembly);
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
        ///   Looks up a localized string similar to SELECT d.domain, d.mxState, h.hostMxRecord, d.lastUpdated, d.error 
        ///FROM mx.MxRecord
        ///JOIN MxHost h
        ///ON h.hostname = MxRecord.hostname
        ///JOIN Domain d
        ///ON d.domain = MxRecord.domain
        ///WHERE d.domain IN ({0});
        ///    .
        /// </summary>
        public static string GetMxRecords {
            get {
                return ResourceManager.GetString("GetMxRecords", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT hostname, preference FROM MxRecord where domain = @domainId;
        ///    .
        /// </summary>
        public static string GetPreference {
            get {
                return ResourceManager.GetString("GetPreference", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT hostEntity.hostname, hostEntity.ipAddress, ipEntity.json as ipJson, hostEntity.json as hostJson
        ///FROM MxRecord mr
        ///JOIN mx.SimplifiedTlsEntity hostEntity 
        ///  ON mr.hostname = hostEntity.hostname
        ///LEFT JOIN mx.SimplifiedTlsEntity ipEntity
        ///  ON hostEntity.ipAddress = ipEntity.ipAddress
        ///  AND ipEntity.hostname = &apos;*&apos;
        ///WHERE mr.domain = @domainId;
        ///    .
        /// </summary>
        public static string GetSimplifiedTlsEntityStates {
            get {
                return ResourceManager.GetString("GetSimplifiedTlsEntityStates", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT `TlsEntity`.`hostname`, `TlsEntity`.`state`
        ///FROM `mx`.`TlsEntity`
        ///WHERE `TlsEntity`.`hostname` IN ({0})
        ///    .
        /// </summary>
        public static string GetTlsEntityStates {
            get {
                return ResourceManager.GetString("GetTlsEntityStates", resourceCulture);
            }
        }
    }
}
