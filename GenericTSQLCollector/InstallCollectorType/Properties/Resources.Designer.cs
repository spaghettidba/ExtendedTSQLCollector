﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sqlconsulting.DataCollector.InstallCollectorType.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Sqlconsulting.DataCollector.InstallCollectorType.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to &lt;xsl:stylesheet xmlns:xsl=&quot;http://www.w3.org/1999/XSL/Transform&quot; xmlns:z=&quot;#RowsetSchema&quot; version=&quot;1.0&quot;&gt;
        ///  &lt;xsl:template match=&quot;/ExtendedXEReaderCollector&quot;&gt;
        ///    &lt;HTML&gt;
        ///      &lt;HEAD&gt;
        ///        &lt;TITLE /&gt;
        ///      &lt;/HEAD&gt;
        ///      &lt;BODY&gt;
        ///        &lt;xsl:apply-templates select=&quot;Session&quot; /&gt;
        ///        &lt;HR /&gt;
        ///        &lt;xsl:apply-templates select=&quot;Alert&quot; /&gt;
        ///      &lt;/BODY&gt;
        ///    &lt;/HTML&gt;
        ///  &lt;/xsl:template&gt;
        ///  &lt;xsl:template match=&quot;Session&quot;&gt;
        ///    &lt;H2&gt;Session&lt;/H2&gt;
        ///    &lt;BR /&gt;	  
        ///	&lt;B&gt;Output Table:&lt;/B&gt;
        ///	&lt;BR /&gt;
        ///	&lt;I&gt;
        ///      &lt;xsl [rest of string was truncated]&quot;;.
        /// </summary>
        public static string XEReaderParamFormatter {
            get {
                return ResourceManager.GetString("XEReaderParamFormatter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;xs:schema xmlns:xs=&quot;http://www.w3.org/2001/XMLSchema&quot; targetNamespace=&quot;DataCollectorType&quot;&gt;
        ///  &lt;xs:element name=&quot;ExtendedXEReaderCollector&quot;&gt;
        ///    &lt;xs:complexType&gt;
        ///      &lt;xs:sequence&gt;
        ///        &lt;xs:element name=&quot;Session&quot; minOccurs=&quot;1&quot; maxOccurs=&quot;1&quot;&gt;
        ///          &lt;xs:complexType&gt;
        ///            &lt;xs:sequence&gt;
        ///              &lt;xs:element name=&quot;Name&quot; type=&quot;xs:string&quot; /&gt;
        ///              &lt;xs:element name=&quot;OutputTable&quot; type=&quot;xs:string&quot; /&gt;
        ///              &lt;xs:element name=&quot;Definition&quot; type=&quot;xs:string&quot; /&gt;
        ///              &lt;xs [rest of string was truncated]&quot;;.
        /// </summary>
        public static string XEReaderParamSchema {
            get {
                return ResourceManager.GetString("XEReaderParamSchema", resourceCulture);
            }
        }
    }
}
