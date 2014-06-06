<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:z="#RowsetSchema" version="1.0">
  <xsl:template match="/ExtendedXEReaderCollector">
    <HTML>
      <HEAD>
        <TITLE />
      </HEAD>
      <BODY>
        <xsl:apply-templates select="Session" />
        <HR />
        <xsl:apply-templates select="Alert" />
      </BODY>
    </HTML>
  </xsl:template>
  <xsl:template match="Session">
    <H2>Session</H2>
    <BR />	  
	<B>Output Table:</B>
	<BR />
	<I>
      <xsl:value-of select="OutputTable" />
    </I>
    <BR />
    <B>Definition:</B>
	<BR />
    <PRE>
      <xsl:value-of select="Definition" />
    </PRE>
	<BR />
    <B>Filter:</B>
    <BR />
	<CODE>
		<xsl:value-of select="Filter" />
	</CODE>
	<BR />
    <B>Columns:</B>
    <BR />
    <CODE>
        <xsl:value-of select="ColumnsList" />
    </CODE>
  </xsl:template>
  <xsl:template match="Alert">
    <H2>Alert</H2>
	<BR />
    <B>Enabled:</B>
    <BR />
    <I>
      <xsl:value-of select="@Enabled" />
    </I>
    <BR />
    <B>Recipient:</B>
    <BR />
    <CODE>
      <xsl:value-of select="Recipient" />
    </CODE>
    <BR />
    <B>Subject:</B>
    <BR />
    <CODE>
      <xsl:value-of select="Subject" />
    </CODE>
    <BR />
    <B>Importance:</B>
    <BR />
    <CODE>
  	  <xsl:value-of select="Importance" />
    </CODE>
    <BR />
    <B>Filter:</B>
    <BR />
    <CODE>
      <xsl:value-of select="Filter" />
    </CODE>
    <BR />
    <B>Delay:</B>
    <BR />
    <CODE>
  	  <xsl:value-of select="Delay" />
    </CODE>
    <BR />
    <B>Write to ERRORLOG:</B>
    <BR />
    <CODE>
  	  <xsl:value-of select="@WriteToERRORLOG" />
    </CODE>
    <BR />
    <B>Write to Windows Log:</B>
    <BR />
    <CODE>
  	  <xsl:value-of select="@WriteToWindowsLog" />
    </CODE>
    <BR />
    <B>Columns:</B>
    <BR />
    <CODE>
      <xsl:value-of select="ColumnsList" />
    </CODE>
    <BR />
    <B>Mode:</B>
    <BR />
    <CODE>
      <xsl:value-of select="Mode" />
    </CODE>
  </xsl:template>
</xsl:stylesheet>