using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Rhino.Geometry;

namespace DendroGH {
    /// <summary>
    /// a DendroMask can be used to execute volume operations to isolated areas. 
    /// a DendroMask has its own volume which represents the masks area of influence.
    /// operations this can be used with are blend, offset, and smooth.
    /// </summary>
    public class DendroMask : IDisposable {
#region Members
        private DendroVolume mVolume; // volume representing the mask
        private double mMin; // minimum value of the mask to be used for the derivation of a smooth alpha value
        private double mMax; // maximum value of the mask to be used for the derivation of a smooth alpha value
        private bool mInvert; // invert mask values
#endregion Members

#region Constructors
        /// <summary>
        /// default constructor
        /// </summary>
        public DendroMask () {
            this.Volume = new DendroVolume ();
            this.Min = 0;
            this.Max = 0;
            this.Invert = false;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="mask">mask to copy from</param>
        public DendroMask (DendroMask mask) {
            this.Volume = new DendroVolume (mask.Volume);
            this.Min = mask.Min;
            this.Max = mask.Max;
            this.Invert = mask.Invert;
        }

        /// <summary>
        /// volume constructor
        /// </summary>
        /// <param name="vCopy">volume to copy from</param>
        public DendroMask (DendroVolume vCopy) {
            this.Volume = new DendroVolume (vCopy);
            this.Min = 0;
            this.Max = 0;
            this.Invert = false;
        }

        /// <summary>
        /// dispose of mask and release resources
        /// </summary>
        public void Dispose () {
            Dispose (true);
        }

        /// <summary>
        /// protected implementation of dispose pattern
        /// </summary>
        /// <param name="bDisposing">holds value indicating if this was called from dispose or finalizer</param>
        protected virtual void Dispose (bool bDisposing) {
                this.Volume.Dispose ();

                if (bDisposing) {
                    GC.SuppressFinalize (this);
                }
            }

            /// <summary>
            /// destructor
            /// </summary>
            ~DendroMask () {
                Dispose (false);
            }
#endregion Constructors

#region Properties
        /// <summary>
        /// mask volume property
        /// </summary>
        /// <remark>area/space where volume operations will be applied</remark>
        /// <returns>volume representing the mask</returns>
        public DendroVolume Volume {
            get {
                return this.mVolume;
            }
            private set
            {
                this.mVolume = value;
            }
        }

        /// <summary>
        /// minimum mask property
        /// </summary>
        /// <remark>essentially this is the blending amount and is the lower limit of graidation</remark>
        /// <returns>minimum value of the mask to be used for the derivation of a smooth alpha value</returns>
        public double Min {
            get {
                return this.mMin;
            }
            set {
                this.mMin = value;
            }
        }

        /// <summary>
        /// maximum mask property
        /// </summary>
        /// <remark>essentially this is the blending amount and is the upper limit of graidation</remark>
        /// <returns>maximum value of the mask to be used for the derivation of a smooth alpha value</returns>
        public double Max {
            get {
                return this.mMax;
            }
            set {
                this.mMax = value;
            }
        }

        /// <summary>
        /// invert mask property
        /// </summary>
        /// <remark>invert value decides whether mask influences inside its volume or outside</remark>
        /// <returns>invert mask value</returns>
        public bool Invert {
            get {
                return this.mInvert;
            }
            set {
                this.mInvert = value;
            }
        }

        /// <summary>
        /// mask validity property
        /// </summary>
        /// <returns>boolean value of mask validity</returns>
        public bool IsValid {
            get {
                return this.Volume.IsValid;
            }
        }

        /// <summary>
        /// mask mesh represention for visualization
        /// </summary>
        /// <returns>mesh representation of mask</returns>
        public Mesh Display {
            get {
                return this.Volume.Display;
            }
        }
#endregion Properties

        /// <summary>
        /// gets the world axis aligned bounding box for the mask volume
        /// </summary>
        /// <returns>boundingbox of the geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox () {
            return this.Volume.Display.GetBoundingBox (true);
        }

        /// <summary>
        /// gets the world axis aligned bounding box for the transformed mask volume
        /// </summary>
        /// <param name="xform">transformation to apply to object prior to the bounding box computation</param>
        /// <returns>accurate boundingbox of the transformed geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox (Transform xform) {
            return this.Volume.Display.GetBoundingBox (xform);
        }

        /// <summary>
        /// transform mask volume from supplied matrix
        /// </summary>
        /// <param name="xform">transform to apply to mask volume</param>
        /// <returns>boolean value for whether transform was successful</returns>
        public bool Transform (Transform xform) {
            return this.Volume.Transform (xform);
        }
    }
}