<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                              xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
                              xmlns:snf="sn://SenseNet.Portal.UI.XmlFormatTools"
                xmlns:snc="sn://SenseNet.Portal.UI.ContentTools">
  
  <xsl:output method="html" indent="yes"/>

  <xsl:template match="/">
    <xsl:apply-templates select="Content"/>
  </xsl:template>
  
  <xsl:template match="Content">
    <table style="width:100%;height:290px">
      <tr style="height:100%">
        <td valign="top">
          <h4>
            <xsl:value-of select="Fields/DisplayName" disable-output-escaping="no"/>
            <xsl:text xml:space="preserve"> (</xsl:text>
            <a target="_blank">
              <xsl:attribute name="href">
                <xsl:text xml:space="preserve">/Explore.html#</xsl:text>
                <xsl:value-of select="ContentTypePath" />
              </xsl:attribute>
              <xsl:attribute name="title">
                <xsl:variable name="explore" select="snc:GetResourceString('$Renderers, Explore')" />
                <xsl:variable name="contenttype" select="snc:GetResourceString('$Renderers, ContentType')" />
                <xsl:text xml:space="preserve">{$explore}</xsl:text>
                <xsl:value-of select="ContentType" />
                <xsl:text xml:space="preserve"> {$contenttype}</xsl:text>
              </xsl:attribute>
              <xsl:value-of select="ContentType"/>              
            </a>
            <xsl:text xml:space="preserve">)</xsl:text>
          </h4>
          <xsl:variable name="creationdate" select="snc:GetResourceString('$Renderers, CreationDate')" />
          
          <h5>
            <xsl:value-of select="$creationdate"/>  <xsl:value-of select="snf:FormatDate(Fields/CreationDate)" />
          </h5>
          </td>
        <td>
          <xsl:variable name="preview" select="snc:GetResourceString('$Renderers, Preview')" />
          <xsl:value-of select="$preview"/><br />
          <iframe src="{SelfLink}" scrolling="no" style="width:100%;height:100%;zoom:50%">

          </iframe>
        </td>
      </tr>
    </table>
  </xsl:template>

  <xsl:template match="*" />
  
</xsl:stylesheet>
