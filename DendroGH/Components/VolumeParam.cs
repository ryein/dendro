using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeParam : GH_Param<VolumeGOO>, IGH_PreviewObject {
        /// <summary>
        /// Initializes a new instance of the VolumeParam class.
        /// </summary>
        public VolumeParam () : base (new GH_InstanceDescription ("Volume", "Volume", "Contains a collection of Volumes", "Dendro", "_Types")) { }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_param_vox;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("f363676f-3ae9-48ac-ab4d-258d8d09e2a7"); }
        }

        bool _hidden;
        public bool Hidden {
            get {
                return _hidden;
            }
            set {
                _hidden = value;
            }
        }

        public bool IsPreviewCapable {
            get { return true; }
        }

        public BoundingBox ClippingBox {
            get { return Preview_ComputeClippingBox (); }
        }

        public void DrawViewportMeshes (IGH_PreviewArgs args) {
            if (args.Document.PreviewMode == GH_PreviewMode.Shaded &&
                args.Display.SupportsShading) {
                Preview_DrawMeshes (args);
            }
        }

        public void DrawViewportWires (IGH_PreviewArgs args) {

        }
    }
}