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

        public SelectionColumn SelectionColumn { get; set; }

        public List<Button> Buttons { get; set; }

        public ServiceColumnsConfig(string indexerField = "Id")
        {
            IndexerField = indexerField;
        }
    }

    public class SelectionColumn {
        public string SelectAllCallbackController { get; set; }

        public string SelectAllCallbackAction { get; set; }

        public bool AllowMultipleSelection { get; set; }
    }

    public abstract class Button
    {
        public string Name { get; set; }

        public string CallbackController { get; set; }

        public string CallbackAction { get; set; }

        public Button(string name, string callbackController, string callbackAction)
        {
            Name = name;
            CallbackController = callbackController;
            CallbackAction = callbackAction;
        }
    }

    public class EditButton : Button
    {
        public string ModalTitle { get; set; }

        public string ModalCancelButtonText { get; set; }

        public string ModalSaveButtonText { get; set; }

        public List<string> NonEditableColumns { get; set; }

        public EditButton(string name, string callbackController, string callbackAction) 
                          : base(name, callbackController, callbackAction)
        {
            ModalTitle = "Edit";
            ModalCancelButtonText = "Cancel";
            ModalSaveButtonText = "Save";
        } 
    }

    public class DeleteButton : Button
    {
        public string ModalTitle { get; set; }

        public string ModalWarningText { get; set; }

        public string ModalCancelButtonText { get; set; }

        public string ModalConfirmButtonText { get; set; }

        public DeleteButton(string name, string callbackController, string callbackAction)
                         : base(name, callbackController, callbackAction)
        {
            ModalTitle = "Delete";
            ModalCancelButtonText = "Cancel";
            ModalConfirmButtonText = "Delete";
        }
    }

    public class CustomButton : Button
    {
        public CustomButton(string name, string callbackController, string callbackAction) 
                            : base(name, callbackController, callbackAction) { }
    }
}
