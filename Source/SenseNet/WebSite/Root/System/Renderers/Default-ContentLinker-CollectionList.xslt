<?xml version="1.0" encoding="utf-8"?>
<xsl:transform version="1.0" 
               xmlns:xsl="http://www.w3.org/1999/XSL/Transform"  
               xmlns:snc="sn://SenseNet.Portal.UI.ContentTools"
                exclude-result-prefixes="xsl snc">
  <xsl:output method="html" indent="yes" omit-xml-declaration="yes" />
  
	  <xsl:template match="/">
		<div>
            <table>
               <tr>
                 <xsl:variable name="content" select="snc:GetResourceString('$Renderers,Content')" />
                 <xsl:variable name="path" select="snc:GetResourceString('$Renderers,Path')" />
                  <td width="150px"><b>
                    <xsl:value-of select="$content" />
                  </b></td>
                  <td><b>
                    <xsl:value-of select="$path" />
                  </b></td>
               </tr>
               <xsl:for-each select="/Content/Children/Content">
                  <tr>
                     <td width="150px"><xsl:value-of select="Fields/DisplayName" disable-output-escaping="no" /></td>
                     <td><xsl:value-of select="Fields/Path" /></td>
                  </tr>
               </xsl:for-each>
            </table>
		</div>
	  </xsl:template>
  
</xsl:transform> 
