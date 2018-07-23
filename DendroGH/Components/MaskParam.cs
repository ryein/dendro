using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class MaskParam : GH_Param<MaskGOO>, IGH_PreviewObject {
        /// <summary>
        /// Initializes a new instance of the MaskParam class.
        /// </summary>
        public MaskParam () : base (new GH_InstanceDescription ("Mask Volume", "vMask", "Contains a collection of Volume Masks", "Dendro", "_Types")) { }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_param_mask;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("f23f32df-3a90-4ff0-8b21-023b5f4f1175"); }
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