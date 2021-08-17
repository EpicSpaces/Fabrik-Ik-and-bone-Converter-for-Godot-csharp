using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class IK_FABRIK : Spatial
{
	[Export]
	public bool start = true;

	[Export]
	NodePath skeleton_path;
	[Export]
	NodePath target_path;
	[Export]
	NodePath tip_path;

	float CHAIN_TOLERANCE = 0.01f;
	Vector3 tmp_tip_rotation;
	Vector3 tmp_middle_position;
	[Export]
	int CHAIN_MAX_ITER = 2;
	int chain_iterations = 0;

	[Export] string[] bones_in_chain;
	float[] bones_in_chain_lengths;

	Spatial target = null;

	Skeleton skeleton;

	Dictionary<string, int> bone_IDs;
	Dictionary<int, Spatial> bone_nodes;

	Vector3 chain_origin = new Vector3();
	float total_length = Mathf.Inf;
	
	Spatial middle_joint_target = null;

	// Called when the node enters the scene tree for( the first time.
	public override void _Ready()
	{
		bone_IDs = new Dictionary<string, int>();
		bone_nodes = new Dictionary<int, Spatial>();
		tmp_tip_rotation = new Vector3();
		tmp_middle_position = new Vector3();
	}
	Spatial converted_target;
	Spatial converted_tip_target;
	public override void _Process(float delta)
	{
		if (skeleton == null)
		{
			skeleton = GetNode(skeleton_path) as Skeleton;
			target = GetNode(target_path) as Spatial;
			middle_joint_target = GetNode(tip_path) as Spatial;
			_make_bone_nodes();
		}
		converted_target = new Spatial();
		converted_tip_target = new Spatial();
		Transform tcv = target.Transform;
		Transform tmcv = middle_joint_target.Transform;

		Spatial p =GetNode("Target2") as Spatial;
		tcv.origin= p.Transform.origin+target.Transform.origin;
		tmcv.origin= p.Transform.origin+middle_joint_target.Transform.origin;
		converted_target.Transform = tcv;
		converted_tip_target.Transform = tmcv;
		converted_target.Rotation = p.Rotation + target.Rotation;
		converted_tip_target.Rotation = p.Rotation + middle_joint_target.Rotation;
		converted_target.Scale = new Vector3(1, 1, 1);
		converted_tip_target.Scale = new Vector3(1, 1, 1);
		// Set all of the bone IDs in bone_IDs, if they are not already made
		int i = 0;
		if (bone_IDs.Count <= 0)
		{
			for (int j = 0; j < bones_in_chain.Length; j++)
			{
				bone_IDs[bones_in_chain[j]] = skeleton.FindBone(bones_in_chain[j]);
				// Set the bone node to the currect bone position
				bone_nodes[i].Transform = get_bone_transform(i);

				// if this is not the last bone in the bone chain, make it look at the next bone in the bone chain
				if (i < bone_IDs.Count - 1)
				{
					bone_nodes[i].LookAt(get_bone_transform(i + 1).origin + skeleton.Transform.origin, Vector3.Up);
					i += 1;
				}
			}
			for (int k = 0; k < bones_in_chain.Length - 1; k++)
			{
				Transform t = get_bone_transform(k + 1, true);
				Transform t2 = get_bone_transform(k, true);
				bones_in_chain_lengths[k] = (t.origin - t2.origin).Length();
			}
		}
		// Set the total length of the bone chain, if( it is not already set
		if (total_length == Mathf.Inf)
		{
			float total_length = 0;
			foreach (float bone_length in bones_in_chain_lengths)
			{
				total_length += bone_length;
			}
		}
		if(start) 
		{
			// Solve the bone chain
			solve_chain();
		} 
	}
	void solve_chain()
	{
		chain_iterations = 0;
		
		// Update the origin with the current bone's origin
		chain_origin = get_bone_transform(0).origin;

		// Get the direction of the final bone by using the next to last bone if( there is more than 2 bones.
		// if( there are only 2 bones, we use the target's forward Z vector instead (not ideal, but it works fairly well)
		Vector3 dir;
		if (bone_nodes.Count > 2)
		{
			dir = bone_nodes[bone_nodes.Count - 2].Transform.basis.z.Normalized();
		}
		else
		{
			dir = -converted_target.Transform.basis.z.Normalized();
		}
		// Get the target position (accounting for the final bone and it's length)
		Vector3 target_pos = converted_target.Transform.origin + (dir * bones_in_chain_lengths[bone_nodes.Count - 1]);

		if (bone_nodes.Count > 2)
		{
			Vector3 middle_point_pos = converted_tip_target.Transform.origin;
			Vector3 middle_point_pos_diff = (middle_point_pos - bone_nodes[bone_nodes.Count / 2].Transform.origin);

			Transform t = bone_nodes[bone_nodes.Count / 2].Transform;
			t.origin += middle_point_pos_diff.Normalized();
			bone_nodes[bone_nodes.Count / 2].Transform = t;
		}
		// Get the difference between our end effector (the final bone in the chain) and the target
		float diff = (bone_nodes[bone_nodes.Count - 1].Transform.origin - target_pos).Length();

		// Check to see if( the distance from the end effector to the target is within our error margin (CHAIN_TOLERANCE).
		// if( it not, move the chain towards the target (going forwards, backwards, and then applying rotation)
		while (diff > CHAIN_TOLERANCE || !tmp_tip_rotation.Equals(converted_target.Rotation) || !tmp_middle_position.Equals(converted_tip_target.Transform.origin))
		{
			chain_backward();
			chain_forward();
			chain_apply_rotation();

			// Update the difference between our end effector (the final bone in the chain) and the target
			diff = (bone_nodes[bone_nodes.Count - 1].Transform.origin - target_pos).Length();
			tmp_tip_rotation = converted_target.Rotation;
			tmp_middle_position = converted_tip_target.Transform.origin;

			// Add one to chain_iterations. if we have reached our max iterations, then break
			chain_iterations = chain_iterations + 1;
			if (chain_iterations >= CHAIN_MAX_ITER)
			{
				break;
			}
		}
		// Reset the bone node transforms to the skeleton bone transforms
		for (int i = 0; i < bone_nodes.Count; i++)
		{
			Transform reset_bone_trans = get_bone_transform(i);
			bone_nodes[i].Transform = reset_bone_trans;
		}
	}
	// Backward reaching pass
	void chain_backward()
	{
		// Get the direction of the final bone by using the next to last bone if( there is more than 2 bones.
		// if( there are only 2 bones, we use the target's for(ward Z vector instead (not ideal, but it works fairly well)
		Vector3 dir;
		if (bone_nodes.Count > 2)
		{
			dir = bone_nodes[bone_nodes.Count - 2].Transform.basis.z.Normalized();
		}
		else
		{
			dir = -converted_target.Transform.basis.z.Normalized();
		}
		// Set the position of the end effector (the final bone in the chain) to the target position
		Transform t = bone_nodes[bone_nodes.Count - 1].Transform;
		t.origin = converted_target.Transform.origin + (dir * bones_in_chain_lengths[bone_nodes.Count - 1]);
		bone_nodes[bone_nodes.Count - 1].Transform = t;

		// for( all of the other bones, move them towards the target
		int i = bones_in_chain.Length - 1;
		while (i >= 1)
		{
			Vector3 prev_origin = bone_nodes[i].Transform.origin;
			i -= 1;
			Vector3 curr_origin = bone_nodes[i].Transform.origin;

			Vector3 r = prev_origin - curr_origin;
			float l = bones_in_chain_lengths[i] / r.Length();
			// Apply the new joint position
			Transform t2 = bone_nodes[i].Transform;
			t2.origin = prev_origin.LinearInterpolate(curr_origin, l);
			bone_nodes[i].Transform = t2;
		}
	}
	void chain_forward()
	{
		// Set root at initial position
		Transform t = bone_nodes[0].Transform;
		t.origin = chain_origin;
		bone_nodes[0].Transform = t;

		// Go through every bone in the bone chain

		for (int i = 0; i < bones_in_chain.Length-1; i++)
		{
			Vector3 curr_origin = bone_nodes[i].Transform.origin;
			Vector3 next_origin = bone_nodes[i + 1].Transform.origin;

			Vector3 r = next_origin - curr_origin;
			float l = bones_in_chain_lengths[i] / r.Length();
			// Apply the new joint position, (potentially with constraints), to the bone node
			Transform t2 = bone_nodes[i + 1].Transform;
			t2.origin = curr_origin.LinearInterpolate(next_origin, l);
			bone_nodes[i + 1].Transform = t2;
		}
	}
	// Make all of the bones rotated correctly.
	void chain_apply_rotation()
	{
		for (int i = 0; i < bones_in_chain.Length; i++)
		{   
			Transform bone_trans = get_bone_transform(i, false);
			Transform b_target = bone_nodes[i].Transform;
		//	b_target.origin = skeleton.GlobalTransform.XformInv(b_target.origin);
			
			if (i == bones_in_chain.Length - 1)
			{
				if (bones_in_chain.Length > 2)
				{
					bone_trans.basis = converted_target.Transform.basis;
					bone_trans.origin = b_target.origin;
				}
				else
				{   //2 bones only
					bone_trans.basis = converted_target.Transform.basis;
					bone_trans.origin = b_target.origin;
				}
			}
			else
			{
				// Get the bone node for this bone, and the next bone
				Transform b_target_two = bone_nodes[i + 1].Transform;

				// Convert the bone nodes positions from world space to bone/skeleton space
		//		b_target_two.origin = skeleton.GlobalTransform.XformInv(b_target_two.origin);

				// Get the direction towards the next bone
				Vector3 dir = (b_target_two.origin - b_target.origin).Normalized();

				// Make this bone look towards the direction of the next bone
			//	bone_trans = bone_trans.LookingAt(b_target.origin + dir, Vector3.Up);

				bone_trans.origin = b_target.origin;
			}
			skeleton.SetBoneGlobalPoseOverride(bone_IDs[bones_in_chain[i]], bone_trans, 1.0f, true);
		}
	}

	Transform get_bone_transform(int bone, bool convert_to_world_space = true)
	{
		Transform ret = skeleton.GetBoneGlobalPose(bone_IDs[bones_in_chain[bone]]);

		if (convert_to_world_space)
		{
		//	ret.origin = skeleton.GlobalTransform.Xform(ret.origin);
		}
		return ret;
	}

	void _make_bone_nodes()
	{
		bones_in_chain_lengths =new float[bones_in_chain.Length];
		for (int i = 0; i < bones_in_chain.Length; i++)
		{
			string bone_name = bones_in_chain[i];
			if (!HasNode(bone_name))
			{
				Spatial new_node = new Spatial();
				bone_nodes[i] = new_node;
				AddChild(bone_nodes[i]);
			}
			else
			{
				bone_nodes[i] = GetNode(bone_name) as Spatial;
			}
		}
	}
}
