﻿using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Validation;
using Fasterflect;
using Xpand.Persistent.Base.Security;

namespace Xpand.ExpressApp.Security.Registration {
    public class ManageUsersOnLogonController : ViewController<DetailView>,IModelExtender {
        public event HandledEventHandler CustomProccessLogonParameter;
        public event EventHandler<CustomActiveKeyArgs> CustomActiveKey;

        protected virtual void OnCustomActiveKey(CustomActiveKeyArgs e) {
            var handler = CustomActiveKey;
            if (handler != null) handler(this, e);
        }


        protected virtual void OnCustomProccessLogonParameter(HandledEventArgs e) {
            var handler = CustomProccessLogonParameter;
            if (handler != null) handler(this, e);
        }

        protected const string LogonActionParametersActiveKey = "Active for ILogonActionParameters only";
        public const string EmailPattern = @"^[_a-z0-9-]+(\.[_a-z0-9-]+)*@[a-z0-9-]+(\.[a-z0-9-]+)*(\.[a-z]{2,4})$";
        private readonly SimpleAction _restorePassword;
        private readonly SimpleAction _registerUser;
        public ManageUsersOnLogonController() {
            _registerUser = CreateLogonSimpleAction("RegisterUser", "RegisterUserCategory", "Register User", "BO_User", "Register a new user within the system", typeof(RegisterUserParameters));
            _restorePassword = CreateLogonSimpleAction("RestorePassword", "RestorePasswordCategory", "Restore Password", "Action_ResetPassword", "Restore forgotten login information", typeof(RestorePasswordParameters));
        }
        
        protected override void OnViewChanging(View view) {
            base.OnViewChanging(view);
            var customActiveKeyArgs = new CustomActiveKeyArgs(view);
            OnCustomActiveKey(customActiveKeyArgs);
            if (!customActiveKeyArgs.Handled)
                Active[ControllerActiveKey] = !SecuritySystem.IsAuthenticated;
        }
        
        protected override void OnViewControlsCreated() {
            base.OnViewControlsCreated();
            bool logonParametersActiveState = GetLogonParametersActiveState();
            foreach (Controller item in Frame.Controllers) {
                var logonController = item as LogonController;
                if (logonController != null) {
                    logonController.AcceptAction.Active[LogonActionParametersActiveKey] = !logonParametersActiveState;
                    logonController.CancelAction.Active[LogonActionParametersActiveKey] = !logonParametersActiveState;
                } else {
                    var dialogController = item as RegistrationDialogController;
                    if (dialogController != null) {
                        dialogController.AcceptAction.Active[LogonActionParametersActiveKey] = logonParametersActiveState;
                        dialogController.CancelAction.Active[LogonActionParametersActiveKey] = logonParametersActiveState;
                        ConfigureDialogController(dialogController);
                    }
                }
            }
        }
        
        private SimpleAction CreateLogonSimpleAction(string id, string category, string caption, string imageName, string toolTip, Type parametersType) {
            var action = new SimpleAction(this, id, category) {
                Caption = caption,
                ImageName = imageName,
                PaintStyle = ActionItemPaintStyle.Image,
                ToolTip = toolTip
            };
            action.Execute += CreateParametersView;
            action.Tag = parametersType;
            return action;
        }
        
        private void CreateParametersView(object sender, SimpleActionExecuteEventArgs e) {
            Application.CallMethod("EnsureShowViewStrategy",Flags.InstancePrivate);
            CreateParametersViewCore(e);
        }
        
        protected virtual void CreateParametersViewCore(SimpleActionExecuteEventArgs e) {
            var parametersType = e.Action.Tag as Type;
            Guard.ArgumentNotNull(parametersType, "parametersType");
            if (parametersType != null) {
                var detailView = Application.CreateDetailView(Application.CreateObjectSpace(), parametersType.CreateInstance());
                detailView.ViewEditMode = ViewEditMode.Edit;
                e.ShowViewParameters.CreatedView = detailView;
            }

            e.ShowViewParameters.Context = TemplateContext.PopupWindow;
            e.ShowViewParameters.TargetWindow = TargetWindow.Current;
            e.ShowViewParameters.Controllers.Add(new RegistrationDialogController());
        }

        protected virtual void ConfigureDialogController(DialogController dialogController) {
            dialogController.AcceptAction.Execute -= AcceptAction_Execute;
            dialogController.CancelAction.Execute -= CancelAction_Execute;
            dialogController.AcceptAction.Executing -= AcceptActionOnExecuting;
            dialogController.AcceptAction.Executing += AcceptActionOnExecuting;
            dialogController.AcceptAction.Execute += AcceptAction_Execute;
            dialogController.CancelAction.Execute += CancelAction_Execute;
        }

        void AcceptActionOnExecuting(object sender, CancelEventArgs cancelEventArgs) {
            var view = ((DialogController) ((ActionBase) sender).Controller).Window.View;
            var currentObject = view.CurrentObject;
            Validator.RuleSet.Validate(view.ObjectSpace,currentObject,ContextIdentifier.Save);
        }
        
        private void AcceptAction_Execute(object sender, SimpleActionExecuteEventArgs e) {
            AcceptParameters(e.CurrentObject as ILogonParameters);
        }
        
        private void CancelAction_Execute(object sender, SimpleActionExecuteEventArgs e) {
            CancelParameters(e.CurrentObject as ILogonParameters);
        }

        protected virtual void AcceptParameters(ILogonParameters parameters) {
            if (parameters != null) {
                var eventArgs = new CustomProcesssLogonParamaterEventArgs(parameters);
                OnCustomProccessLogonParameter(eventArgs);
                if (!eventArgs.Handled)
                    parameters.Process(Application,ObjectSpace);
            }
            Application.LogOff();
        }
        protected virtual void CancelParameters(ILogonParameters parameters) {
            Application.LogOff();
        }
        
        protected virtual bool GetLogonParametersActiveState() {
            return View != null && View.ObjectTypeInfo != null && View.ObjectTypeInfo.Implements<ILogonParameters>();
        }
        public SimpleAction RestorePasswordAction {
            get { return _restorePassword; }
        }
        public SimpleAction RegisterUserAction {
            get { return _registerUser; }
        }
        
        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelOptions, IModelOptionsRegistration>();
            extenders.Add<IModelRegistrationEnabled, IModelRegistration>();
            extenders.Add<IModelRegistrationEnabled, IModelRegistrationActivation>();
        }
    }

    public class CustomActiveKeyArgs:HandledEventArgs {
        readonly View _view;

        public CustomActiveKeyArgs(View view) {
            _view = view;
            Handled = false;
        }

        public View View {
            get { return _view; }
        }
    }

    public class CustomProcesssLogonParamaterEventArgs:HandledEventArgs {
        readonly ILogonParameters _parameters;

        public CustomProcesssLogonParamaterEventArgs(ILogonParameters parameters) {
            _parameters = parameters;
        }

        public ILogonParameters Parameters {
            get { return _parameters; }
        }
    }
}