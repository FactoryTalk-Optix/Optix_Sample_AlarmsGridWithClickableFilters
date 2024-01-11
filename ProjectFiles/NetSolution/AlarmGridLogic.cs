#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.AccessControl;
using FTOptix.EventLogger;
#endregion

public class AlarmGridLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmsDataGridModel = Owner.Get<DataGrid>("AlarmsDataGrid").GetVariable("Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguageVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguageVariable.VariableChange += OnSessionActualLanguageChange;

        alarmSummaryFilter = new AlarmSummaryFilter(Owner);
    }

    public override void Stop()
    {
        actualLanguageVariable.VariableChange -= OnSessionActualLanguageChange;
    }

    public void OnSessionActualLanguageChange(object sender, VariableChangeEventArgs e)
    {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        string dynamicLinkValue = dynamicLink.Value;
        dynamicLink.Value = string.Empty;
        dynamicLink.Value = dynamicLinkValue;
    }

    [ExportMethod]
    public void Filter(string filterName)
    {
        alarmSummaryFilter.ToggleFilterState(filterName);
        alarmSummaryFilter.BuildQuery();
        alarmSummaryFilter.RefreshQuery();
    }

    [ExportMethod]
    public void ClearAll()
    {
        Rectangle rectangle = Owner.Get<Rectangle>("Panel2/Rectangle1");
        var childrens = rectangle.Children;
        foreach (var child in childrens)
        {
            var checkbox = child as CheckBox;
            if (checkbox != null)
                checkbox.Checked = false;
        }

        alarmSummaryFilter.ClearAll();
        alarmSummaryFilter.BuildQuery();
        alarmSummaryFilter.RefreshQuery();
    }

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguageVariable;
    private AlarmSummaryFilter alarmSummaryFilter;
}

public class AlarmSummaryFilter
{
    public AlarmSummaryFilter(IUANode owner)
    {
        alarmDataGridQuery = owner.Get<DataGrid>("AlarmsDataGrid").GetVariable("Query");

        filters = new List<Filter>()
        {
            new(){ uiFilterName = "Active alarms",   uiChecked = false, sqlCondition = "ActiveState = 'True'" },
            new(){ uiFilterName = "Inactive alarms", uiChecked = false, sqlCondition = "ActiveState = 'False'" },
            new(){ uiFilterName = "Alarm State: In Alarm - Confirmed", uiChecked = false, sqlCondition = "ConfirmedState = 'True'" },
            new(){ uiFilterName = "Alarm State: In Alarm - Unconfirmed", uiChecked = false, sqlCondition = "ConfirmedState = 'False'" },
            new(){ uiFilterName = "Alarm State: In Alarm - Acked", uiChecked = false, sqlCondition = "AckedState = 'True'" },
            new(){ uiFilterName = "Alarm State: In Alarm - Unacked", uiChecked = false, sqlCondition = "AckedState = 'False'" }
        };

        query = mandatorySQLpart;
        actualCheckedFiltersCount = 0;
    }

    public void ToggleFilterState(string uiFilterName)
    {
        foreach (var filter in filters)
        {
            if (filter.uiFilterName.Equals(uiFilterName))
            {
                if (filter.uiChecked)
                    actualCheckedFiltersCount--;
                else
                    actualCheckedFiltersCount++;

                filter.uiChecked = !filter.uiChecked;
                break;
            }
        }
    }

    public void BuildQuery()
    {
        if (actualCheckedFiltersCount == 0)
        {
            query = mandatorySQLpart;
            return;
        }

        bool wasWHEREadded = false;
        int checkedCounter = 0;
        query = mandatorySQLpart;

        foreach (var filter in filters)
        {
            if (filter.uiChecked)
            {
                checkedCounter++;

                if (!wasWHEREadded)
                {
                    query += " WHERE ";
                    wasWHEREadded = true;
                }

                query += filter.sqlCondition;

                if (checkedCounter != actualCheckedFiltersCount)
                {
                    query += " OR ";
                }
            }
        }
    }

    public void RefreshQuery()
    {
        alarmDataGridQuery.Value = query;
    }

    public void ClearAll()
    {
        actualCheckedFiltersCount = 0;

        foreach (var filter in filters)
        {
            filter.uiChecked = false;
        }

    }

    private class Filter
    {
        public string uiFilterName;
        public bool uiChecked;
        public string sqlCondition;
    }

    private string query;
    private List<Filter> filters;
    private int actualCheckedFiltersCount;
    const string mandatorySQLpart = "SELECT * FROM Model";
    private IUAVariable alarmDataGridQuery;
}
