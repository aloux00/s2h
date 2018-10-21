<%@ Page language="c#" Codebehind="Status.aspx.cs" AutoEventWireup="false" Inherits="SocksOverHttp.Status" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>WebForm1</title>
		<meta name="GENERATOR" Content="Microsoft Visual Studio .NET 7.1">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<h3>Connections</h3>
			<asp:Table id="Table1" runat="server" CellPadding="2" CellSpacing="2" BorderWidth="1px"></asp:Table>
			<br>
			<h3>Clients</h3>
			<asp:Table id="Table2" runat="server" CellPadding="2" CellSpacing="2" BorderWidth="1px"></asp:Table>
		</form>
	</body>
</HTML>
