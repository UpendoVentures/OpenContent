<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenContent.EditNotifications" CodeBehind="EditNotifications.ascx.cs" %>
<%@ Import Namespace="Newtonsoft.Json" %>

<asp:Panel ID="ScopeWrapper" runat="server">
    <div id="field1" class="alpaca"></div>
    <ul class="dnnActions dnnClear" style="display: block; padding-left: 35%">
        <li>
            <asp:HyperLink ID="cmdSave" runat="server" class="dnnPrimaryAction" resourcekey="cmdSave" />
        </li>
        <li>
            <asp:HyperLink ID="hlCancel" runat="server" class="dnnSecondaryAction" resourcekey="cmdCancel" />
        </li>

    </ul>
</asp:Panel>

<script type="text/javascript">
    $(document).ready(function () {
        var config = <%=JsonConvert.SerializeObject(AlpacaContext)%>;
        config.editAction = "EditSettings";
        config.updateAction = "UpdateSettings";
        config.data = { "key": "notifications", "templateFolder": false};
        var engine = new alpacaEngine.engine(config);
        engine.init();
    });
</script>
