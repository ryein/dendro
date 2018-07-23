using System;

namespace DendroGH {
    /// <summary>
    /// class containing all settings needed for converting different 
    /// geometry types to and from DendroVolume types. Implemented to 
    /// simplify working with multiple DendroVolume objects, allowing 
    /// you to create a single DendroSetting that can be applied to all
    /// </summary>
    public class DendroSettings {

#region Members
        private double mAdaptivity = 0.1; // higher adaptivity will allow more variation in polygon size, resulting in fewer polygons
        private double mBandwidth = 1.0; // desired radius in voxel units around the surface
        private double mIsovalue = 0.01; // crossing point of the volume that is considered the surface
        private double mVoxelSize = 0.5; // size of voxels in the output volume
#endregion Members

#region Constructors
        /// <summary>
        /// default constructor
        /// </summary>
        public DendroSettings () { }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="ds">settings to copy from</param>
        public DendroSettings (DendroSettings ds) {
            this.mAdaptivity = ds.Adaptivity;
            this.mBandwidth = ds.Bandwidth;
            this.mIsovalue = ds.IsoValue;
            this.mVoxelSize = ds.VoxelSize;
        }
#endregion Constructors

#region Properties
        /// <summary>
        /// bandwidth property
        /// </summary>
        /// <returns>bandwidth amount for volume</returns>
        public double Bandwidth {
            get { return this.mBandwidth; }
            set { this.mBandwidth = value; }
        }

        /// <summary>
        /// voxel size property
        /// </summary>
        /// <returns>voxel size for volume</returns>
        public double VoxelSize {
            get { return this.mVoxelSize; }
            set { this.mVoxelSize = value; }
        }

        /// <summary>
        /// isovalue property
        /// </summary>
        /// <returns>isovalue for meshing</returns>
        public double IsoValue {
            get { return this.mIsovalue; }
            set { this.mIsovalue = value; }
        }

        /// <summary>
        /// adaptivity property
        /// </summary>
        /// <returns>adaptivity value for meshing</returns>
        public double Adaptivity {
            get { return this.mAdaptivity; }
            set { this.mAdaptivity = value; }
        }
#endregion Properties
    }
}