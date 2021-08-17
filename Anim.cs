using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class Anim : Spatial
{
	[Export]
	public bool start = true;
	[Export]
	public bool gettingpos = true;

	[Export]
	NodePath skeleton_path;
	
	Skeleton skeleton;

	Dictionary<string, int> bone_IDs;
	Dictionary<int, Spatial> bone_nodes;
	List<int> boi;
	// Called when the node enters the scene tree for( the first time.
	public override void _Ready()
	{
		bone_IDs = new Dictionary<string, int>();
		bone_nodes = new Dictionary<int, Spatial>();
	}
	public override void _Process(float delta)
	{
		if (skeleton == null)
		{
			skeleton = GetNode(skeleton_path) as Skeleton;
		}

		// Set all of the bone IDs in bone_IDs, if they are not already made
		if(gettingpos)
		{
			for (int j = 0; j < skeleton.GetBoneCount(); j++)
			{
				bone_IDs[skeleton.GetBoneName(j)] = skeleton.FindBone(skeleton.GetBoneName(j));
				boi = new List<int>();
				find_p(skeleton.GetBoneParent(j));
				string bone_name = "";
				if (boi.Count > 0)
				{
					for (int k = boi.Count - 1; k >= 0; k--)
					{
						if(k== boi.Count - 1)
						bone_name = skeleton.GetBoneName(boi[k]);
						else
							bone_name += "/" + skeleton.GetBoneName(boi[k]);
					}
				}
				string bone_name_last = "";
				if (bone_name.Equals(""))
				 bone_name_last = skeleton.GetBoneName(j);
				else
				 bone_name_last = bone_name+"/"+skeleton.GetBoneName(j);

				//	GD.Print(bone_name);
				//	GD.Print(bone_name_last);
				if (!skeleton.HasNode(bone_name_last))
				{
					Spatial new_node = new Spatial();
					bone_nodes[j] = new_node;
					bone_nodes[j].Name = skeleton.GetBoneName(j);

					if (bone_name.Equals(""))
					{
						skeleton.AddChild(bone_nodes[j]);
					}
					else
					{
						//		GD.Print(bone_name);
						skeleton.GetNode(bone_name).AddChild(bone_nodes[j]);
					}
					bone_nodes[j].Owner = Owner;
				}
				else
				{
					//	GD.Print(bone_name);
					if (bone_name.Equals(""))
						bone_name = skeleton.GetBoneName(j);
					
					bone_nodes[j] = skeleton.GetNode(bone_name) as Spatial;
				}
				// Set the bone node to the currect bone position
				bone_nodes[j].GlobalTransform = get_bone_transform(j);
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
		chain_apply_rotation();
		// Reset the bone node transforms to the skeleton bone transforms
		for (int i = 0; i < bone_nodes.Count; i++)
		{
			Transform reset_bone_trans = get_bone_transform(i);
			bone_nodes[i].Transform = reset_bone_trans;
		}
	}
	// Backward reaching pass
	// Make all of the bones rotated correctly.
	void chain_apply_rotation()
	{
		for (int i = 0; i < skeleton.GetBoneCount(); i++)
		{
			Transform bone_trans = get_bone_transform(i, false);
			Transform b_target = bone_nodes[i].Transform;
			//	b_target.origin = skeleton.GlobalTransform.XformInv(b_target.origin);

			bone_trans.basis = bone_nodes[i].Transform.basis;
			bone_trans.origin = b_target.origin;
			skeleton.SetBoneGlobalPoseOverride(bone_IDs[skeleton.GetBoneName(i)], bone_trans, 1.0f, true);
		}
	}

	Transform get_bone_transform(int bone, bool convert_to_world_space = true)
	{
		Transform ret = skeleton.GetBoneGlobalPose(bone);

		if (convert_to_world_space)
		{
		//	ret.origin = skeleton.GlobalTransform.Xform(ret.origin);
		}
		return ret;
	}
	void find_p(int b) 
	{
		if (b!=-1) 
		{
			boi.Add(b);
			find_p(skeleton.GetBoneParent(b));

		}
	}
}
