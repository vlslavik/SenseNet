<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:snc="sn://SenseNet.Portal.UI.ContentTools"
                exclude-result-prefixes="xsl snc">
  <xsl:output method="html" indent="yes"/>

  <xsl:template match="/">
      <div class="sn-contentlist">
        <xsl:for-each select="/Content/Children/Content[position() &lt; 5]">
          <div class="sn-content sn-contentlist-item">
            <h1 class="sn-content-title">
              <a href="{Actions/Browse}">
                <xsl:value-of select="Fields/DisplayName" disable-output-escaping="no" />
              </a>
            </h1>
            <div class="sn-content-header"></div>
            <div class="sn-more">
              <xsl:variable name="more" select="snc:GetResourceString('$Renderers, more')" />
              <a class="sn-link sn-content-link" href="{Actions/Browse}">
                <xsl:value-of select="$more"/>>>
              </a>
            </div>
          </div>
        </xsl:for-each>
      </div>
  </xsl:template>

</xsl:stylesheet>
