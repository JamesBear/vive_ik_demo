using UnityEngine;
using System.Collections;

namespace RootMotion {

	/// <summary>
	/// Helper methods for dealing with Quaternions.
	/// </summary>
	public static class QuaTools {

		/// <summary>
		/// Returns the rotation from identity Quaternion to "q", interpolated linearily by "weight".
		/// </summary>
		public static Quaternion LinearBlend(Quaternion q, float weight) {
			if (weight <= 0f) return Quaternion.identity;
			if (weight >= 1f) return q;
			return Quaternion.Lerp(Quaternion.identity, q, weight);
		}

		/// <summary>
		/// Returns the rotation from identity Quaternion to "q", interpolated spherically by "weight".
		/// </summary>
		public static Quaternion SphericalBlend(Quaternion q, float weight) {
			if (weight <= 0f) return Quaternion.identity;
			if (weight >= 1f) return q;
			return Quaternion.Slerp(Quaternion.identity, q, weight);
		}

		/// <summary>
		/// Creates a FromToRotation, but makes sure it's axis remains fixed near to the Quaternion singularity point.
		/// </summary>
		/// <returns>
		/// The from to rotation around an axis.
		/// </returns>
		/// <param name='fromDirection'>
		/// From direction.
		/// </param>
		/// <param name='toDirection'>
		/// To direction.
		/// </param>
		/// <param name='axis'>
		/// Axis. Should be normalized before passing into this method.
		/// </param>
		public static Quaternion FromToAroundAxis(Vector3 fromDirection, Vector3 toDirection, Vector3 axis) {
			Quaternion fromTo = Quaternion.FromToRotation(fromDirection, toDirection);
			
			float angle = 0;
			Vector3 freeAxis = Vector3.zero;
			
			fromTo.ToAngleAxis(out angle, out freeAxis);
			
			float dot = Vector3.Dot(freeAxis, axis);
			if (dot < 0) angle = -angle;
			
			return Quaternion.AngleAxis(angle, axis);
		}
		
		/// <summary>
		/// Gets the rotation that can be used to convert a rotation from one axis space to another.
		/// </summary>
		public static Quaternion RotationToLocalSpace(Quaternion space, Quaternion rotation) {
			return Quaternion.Inverse(Quaternion.Inverse(space) * rotation);
		}

		/// <summary>
		/// Gets the Quaternion from rotation "from" to rotation "to".
		/// </summary>
		public static Quaternion FromToRotation(Quaternion from, Quaternion to) {
			if (to == from) return Quaternion.identity;

			return to * Quaternion.Inverse(from);
		}


		/// <summary>
		/// Gets the closest direction axis to a vector. Input vector must be normalized!
		/// </summary>
		public static Vector3 GetAxis(Vector3 v) {
			Vector3 closest = Vector3.right;
			bool neg = false;

			float x = Vector3.Dot(v, Vector3.right);
			float maxAbsDot = Mathf.Abs(x);
			if (x < 0f) neg = true;

			float y = Vector3.Dot(v, Vector3.up);
			float absDot = Mathf.Abs(y);
			if (absDot > maxAbsDot) {
				maxAbsDot = absDot;
				closest = Vector3.up;
				neg = y < 0f;
			}

			float z = Vector3.Dot(v, Vector3.forward);
			absDot = Mathf.Abs(z);
			if (absDot > maxAbsDot) {
				closest = Vector3.forward;
				neg = z < 0f;
			}

			if (neg) closest = -closest;
			return closest;
 		}
	}
}
