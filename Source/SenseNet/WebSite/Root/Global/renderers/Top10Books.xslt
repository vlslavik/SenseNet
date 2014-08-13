
  <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                  xmlns:sec="sn://SenseNet.Portal.Helpers.Security"
      xmlns:msxsl="urn:schemas-microsoft-com:xslt"
      xmlns:snc="sn://SenseNet.Portal.UI.ContentTools"
                  exclude-result-prefixes="msxsl snc sec"
>

  <xsl:key name="list" match="Content/Children/Content" use="substring(Fields/PublishDate,1,10)"/>
  <xsl:template match="/">
    <ul>
      <xsl:for-each select="Content/Children/Content[generate-id(.)=generate-id(key('list',substring(Fields/PublishDate,1,10)))]/Fields/PublishDate">
        <xsl:sort case-order="upper-first" select="."/>
        <li>
          <span>
            <xsl:variable name="released" select="snc:GetResourceString('$Renderers, Released')" />
            <xsl:value-of select="$released"/><br/>
            <xsl:value-of select="substring(.,1,10)"/>
          </span>
          <ul>

            <xsl:for-each select="key('list', substring(.,1,10))">
              <xsl:sort/>
              <li>
                <xsl:apply-templates select="."/>
              </li>
            </xsl:for-each>

          </ul>
        </li>
      </xsl:for-each>
    </ul>
  </xsl:template>

  <xsl:template match="Content">
    <div>
      <div>
        <a href="{Actions/Details}">
          <img src="alma.jpg" class="almaImg"/>
        </a>
      </div>
      <div>
        <span>
          <xsl:value-of select="Fields/DisplayName" disable-output-escaping="no"/>
        </span>

        <xsl:if test="Fields/IsRateable='True'">
          <span>//TODO: RATING!!!</span>
        </xsl:if>

        <span>
          <xsl:variable name="by" select="snc:GetResourceString('$Renderers, By')" />
          <xsl:value-of select="$by"/>: <xsl:value-of select="Fields/Author" disable-output-escaping="no"/>
        </span>
        <span>
          <xsl:variable name="delete" select="snc:GetResourceString('$Renderers,Delete')" />
          <xsl:variable name="edit" select="snc:GetResourceString('$Renderers, Edit')" />
          <a class="sn-actionlinkbutton icon" href="{Actions/Delete}" style="background-image:url=(/Root/Global/images/icons/16/delete.png);">
            <xsl:value-of select="$delete"/>
          </a>
          <a class="sn-actionlinkbutton icon" href="{Actions/Edit}" style="background-image:url=(/Root/Global/images/icons/16/edit.png);">
            <xsl:value-of select="$edit"/>
          </a>
        </span>
      </div>
    </div>
  </xsl:template>
</xsl:stylesheet>