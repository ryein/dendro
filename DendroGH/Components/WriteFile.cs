using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class WriteFile : GH_Component {
        /// <summary>
        /// Initializes a new instance of the WriteFile class.
        /// </summary>
        public WriteFile () : base ("Write Volume", "vWrite",
            "Write a Volume to a file",
            "Dendro", "IO") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddTextParameter("File Path", "F", "Path of volume file (*.vdb files)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Write?", "W", "Boolean toggle to write file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) { }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume volume = new DendroVolume ();
            string filepath = "";
            bool isWrite = false;

            if (!DA.GetData (0, ref volume)) return;
            if (!DA.GetData (1, ref filepath)) return;
            if (!DA.GetData(2, ref isWrite)) return;

            if (isWrite)
            {
                bool success = volume.Write(filepath);

                if (!success)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Write failed. Make sure you supplied a path plus filename with the extension *.vdb");
                    return;
                }
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_io_out;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("e0ee9551-254b-49d0-b4ec-53238c7e9668"); }
        }
    }
}