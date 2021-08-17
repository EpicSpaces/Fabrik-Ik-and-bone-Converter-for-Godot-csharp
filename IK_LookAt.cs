using Godot;
using System;
[Tool]
public class IK_LookAt : Spatial
{
	[Export]
	NodePath skeleton_path;
	[Export]
	string bone_name = "";
	[Export(PropertyHint.Enum, "X-up, Y-up, Z-up,-X-up, -Y-up, -Z-up")]
	int look_at_axis = 1;
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.001f")]
	float interpolation = 1.0f;
	[Export]
	Vector3 additional_rotation = new Vector3();
	
	Skeleton skeleton_to_use = null;
	public override void _Ready()
	{
	}
	public override void _Process(float delta)
	{
		if (skeleton_to_use == null)
			skeleton_to_use = GetNode(skeleton_path) as Skeleton;

		// If we do not have a skeleton and/or we're not supposed to update, then return.
		if (skeleton_to_use == null)
			return;

		// Get the bone index.
		int bone = skeleton_to_use.FindBone(bone_name);
		Transform rest = skeleton_to_use.GetBoneGlobalPose(bone);

		// Convert our position relative to the skeleton's transform.
		Vector3 target_pos = skeleton_to_use.GlobalTransform.XformInv(GlobalTransform.origin);

		// Call helper's look_at function with the chosen up axis.
		if (look_at_axis == 0)
			rest = rest.LookingAt(target_pos, Vector3.Right);
		else if (look_at_axis == 1)
			rest = rest.LookingAt(target_pos, Vector3.Up);
		else if (look_at_axis == 2)
			rest = rest.LookingAt(target_pos, Vector3.Forward);
		if (look_at_axis == 3)
			rest = rest.LookingAt(target_pos, -Vector3.Right);
		else if (look_at_axis == 4)
			rest = rest.LookingAt(target_pos, -Vector3.Up);
		else if (look_at_axis == 5)
			rest = rest.LookingAt(target_pos, -Vector3.Forward);
		else
			rest = rest.LookingAt(target_pos, Vector3.Up);
		
			// Get the rotation euler of the bone and of this node.
			var rest_euler = rest.basis.GetEuler();
		// Make a new basis with the, potentially, changed euler angles.
		rest.basis = new Basis(rest_euler);
		
		// Apply additional rotation stored in additional_rotation to the bone.
		if (additional_rotation != Vector3.Zero)
		{
			rest.basis = rest.basis.Rotated(rest.basis.x, Mathf.Deg2Rad(additional_rotation.x));
			rest.basis = rest.basis.Rotated(rest.basis.y, Mathf.Deg2Rad(additional_rotation.y));
			rest.basis = rest.basis.Rotated(rest.basis.z, Mathf.Deg2Rad(additional_rotation.z));
		}

		// Finally, apply the new rotation to the bone in the skeleton.
		skeleton_to_use.SetBoneGlobalPoseOverride(bone, rest, interpolation, true);
	}

	float PI = 3.14159265358979323846f;
	float minfloat = 1.17549e-038f;
	float maxfloat = 3.40282e+038f;
	float ToRadian(float x)
	{
		return (float)(((x) * PI / 180.0f));
	}
	float ToDegree(float x)
	{
		return (float)(((x) * 180.0f / PI));
	}
	float Angle(Vector3 From, Vector3 To)
	{
		// sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
		float kEpsilonNormalSqrt = 1e-15F;
		float denominator = (float)Mathf.Sqrt(From.Length() * To.Length());
		if (denominator < kEpsilonNormalSqrt)
			return 0.0f;

		float dot = Mathf.Clamp(From.Dot(To) / denominator, -1.0f, 1.0f);
		return ToDegree((float)Mathf.Acos(dot));
	}
	// dir1 dir2 = need to be unit vectors
	// assumed to start at the exact same point; their direction matters
	// the result is in radians
	public float SignedAngle(Vector3 dir1, Vector3 dir2, Vector3 normal)
	{
		return Mathf.Atan2(dir1.Cross(dir2).Dot(normal), dir1.Dot(dir2));
	}
	Quat AngleAxis(ref Vector3 axis, float theta)
	{
		axis = axis.Normalized();
		float rad = ToRadian(theta) * 0.5f;
		axis *= Mathf.Sin(rad);
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;
		float w = Mathf.Cos(rad);
		return new Quat(x, y, z, w);
	}
	Quat FromToRotation(Vector3 From, Vector3 To)
	{
		Vector3 axis = From.Cross(To);
		float angle = Angle(From, To);

		if (angle >= 180.0f)
		{
			Vector3 r = From.Cross(new Vector3(1.0f, 0.0f, 0.0f));
			axis = r.Cross(From);
			if (axis.Length() < 0.000001f)
				axis = new Vector3(0.0f, 1.0f, 0.0f);
		}
		axis = axis.Normalized();
		return AngleAxis(ref axis, angle);
	}
	Quat toQuat(float x, float y, float z)
	{
		Quat q = new Quat();
		// Assuming the angles are in radians.
		float c1 = Mathf.Cos(x / 2);
		float s1 = Mathf.Sin(x / 2);
		float c2 = Mathf.Cos(y / 2);
		float s2 = Mathf.Sin(y / 2);
		float c3 = Mathf.Cos(z / 2);
		float s3 = Mathf.Sin(z / 2);
		float c1c2 = c1 * c2;
		float s1s2 = s1 * s2;
		q.w = c1c2 * c3 - s1s2 * s3;
		q.z = c1c2 * s3 + s1s2 * c3;
		q.y = s1 * c2 * c3 + c1 * s2 * s3;
		q.x = c1 * s2 * c3 - s1 * c2 * s3;
		return q;
	}

}
