using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Configuration
{
    public class ServiceColumnsConfig
    {
        public string IndexerField { get; set; }

        public bool SelectionColumn { get; set; }

        public string SelectAllCallbackController { get; set; }

        public string SelectAllCallbackAction { get; set; }

        public bool AllowMultipleSelection { get; set; } 

        public Dictionary<string, ServiceColumnCallback> ServiceButtons { get; set; }
    }

    public class ServiceColumnCallback
    {
        public string CallbackController { get; set; }

        public string CallbackAction { get; set; }

        public bool IsEditRow { get; set; }

        public ServiceColumnCallback(string controller, string action, bool isEditRow = false)
        {
            IsEditRow = isEditRow;
            CallbackController = controller;
            CallbackAction = action;
        }
    }
}
