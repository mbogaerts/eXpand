﻿using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Persistent.Base.General.Model;

namespace Xpand.Persistent.Base.General.Controllers {
    public interface IModelDetailViewViewEditMode {
        [Category(AttributeCategoryNameProvider.Xpand)]
        [Description("Control detail view default edit mode")]
        ViewEditMode? ViewEditMode { get; set; }
    }

    public class ViewEditModeController : WindowController, IModelExtender {
        public const string ViewActiveKey = "ViewEditMode";
        private void FrameOnDisposing(object sender, EventArgs eventArgs) {
            Frame.ViewChanging-=FrameOnViewChanging;
            Frame.Disposing -= FrameOnDisposing;
            foreach (var action in Frame.Actions()) {
                action.Executed -= ActionOnExecuted;
            }
        }

        protected override void OnFrameAssigned() {
            base.OnFrameAssigned();
            Frame.ViewChanging+=FrameOnViewChanging;
            Frame.Disposing += FrameOnDisposing;
            foreach (var action in Frame.Actions()) {
                action.Executed += ActionOnExecuted;
            }
        }

        private void FrameOnViewChanging(object sender, ViewChangingEventArgs e){
            var detailView = e.View as DetailView;
            if (detailView != null){
                detailView.ControlsCreated += ViewOnControlsCreated;
                if (!Application.IsHosted()&& ((IModelDetailViewViewEditMode)detailView.Model).ViewEditMode.HasValue)
                    detailView.ObjectSpace.Reloaded += (o, args) => UpdateEditableActions(detailView);
            }
        }

        private void ViewOnControlsCreated(object sender, EventArgs eventArgs) {
            var view = ((View)sender);
            view.ControlsCreated -= ViewOnControlsCreated;
            UpdateView((DetailView)view);
            if (!Application.IsHosted()&& ((IModelDetailViewViewEditMode)view.Model).ViewEditMode.HasValue) {
                UpdateEditableActions(view);
                view.ObjectSpace.Reloaded += (o, args) => UpdateEditableActions(view);
            }
        }

        private void ActionOnExecuted(object sender, ActionBaseEventArgs e) {
            var detailView = e.ShowViewParameters.CreatedView as DetailView;
            if (detailView != null) {
                UpdateView(detailView);
            }
        }

        private void UpdateView(DetailView detailView) {
            var viewEditMode = ((IModelDetailViewViewEditMode)detailView.Model).ViewEditMode;
            if (viewEditMode.HasValue) {
                if (!detailView.ObjectSpace.IsNewObject(detailView.CurrentObject)) {
                    UpdateViewEditModeState(detailView, viewEditMode.Value);
                    UpdateViewAllowEditState(detailView);
                }
            }
        }

        protected virtual void UpdateViewEditModeState(DetailView view, ViewEditMode viewEditMode) {
            view.ViewEditMode = viewEditMode;
        }

        protected virtual void UpdateViewAllowEditState(DetailView view) {
            if (!Application.IsHosted()) {
                view.AllowEdit[ViewActiveKey] = view.ViewEditMode == ViewEditMode.Edit;
            }
        }

        private void UpdateEditableActions(View view) {
            var modificationsController = Frame.GetController<ModificationsController>();
            var key = typeof(ViewEditModeController).Name;
            modificationsController.SaveAction.Active[key] = view.AllowEdit;
            modificationsController.SaveAndCloseAction.Active[key] = view.AllowEdit;
            modificationsController.CancelAction.Active[key] = view.AllowEdit;
            Frame.GetController<DeleteObjectsViewController>().DeleteAction.Active[key] = view.AllowEdit;
            Frame.GetController<NewObjectViewController>().NewObjectAction.Active[key] = view.AllowEdit;
        }

        void IModelExtender.ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelDetailView, IModelDetailViewViewEditMode>();
        }
    }
}