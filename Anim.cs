using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class Anim : Spatial
{
	[Export]
	public bool start = false;
	[Export]
	public bool gettingpos_rest = true;

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
		if(gettingpos_rest)
		{
			gettingpos_rest = false;
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
						if (k == boi.Count - 1)
							bone_name = skeleton.GetBoneName(boi[k]);
						else
							bone_name += "/" + skeleton.GetBoneName(boi[k]);
					}
				}
				string bone_name_last = "";
				if (bone_name.Equals(""))
					bone_name_last = skeleton.GetBoneName(j);
				else
					bone_name_last = bone_name + "/" + skeleton.GetBoneName(j);

				//	GD.Print(bone_name);
				//	GD.Print(bone_name_last);
				if (!HasNode(bone_name_last))
				{
					Spatial new_node = new Spatial();
					bone_nodes[j] = new_node;
					bone_nodes[j].Name = skeleton.GetBoneName(j);
 
					Spatial rest_node = new Spatial();
					rest_node.Name = skeleton.GetBoneName(j);

					if (bone_name.Equals(""))
					{
						AddChild(bone_nodes[j]);
						GetParent().GetNode("Spatial_rest").AddChild(rest_node);
					}
					else
					{
						//		GD.Print(bone_name);
						GetNode(bone_name).AddChild(bone_nodes[j]);
						GetParent().GetNode("Spatial_rest").GetNode(bone_name).AddChild(rest_node);
					}
					bone_nodes[j].Owner = Owner;
					rest_node.Owner = Owner;
					Transform tsk = bone_nodes[j].GlobalTransform;
					tsk = get_bone_transform(j);
					//		tsk.basis.Scale = bone_nodes[j].GlobalTransform.basis.Scale;
					bone_nodes[j].GlobalTransform = tsk;
					rest_node.GlobalTransform = tsk;
				}
				else
				{
					//	GD.Print(bone_name);
					if (bone_name.Equals(""))
						bone_name = skeleton.GetBoneName(j);

					bone_nodes[j] = GetNode(bone_name_last) as Spatial;
					bone_nodes[j].GlobalTransform = (GetParent().GetNode("Spatial_rest").GetNode(bone_name_last) as Spatial).GlobalTransform;
				}
			}
		}
		if(start) 
		{
			solve_chain();
		}
	}
	void solve_chain()
	{
		chain_apply_rotation();
	}
	void chain_apply_rotation()
	{
		for (int i = 0; i < skeleton.GetBoneCount(); i++)
		{
			Transform b_target = bone_nodes[i].GlobalTransform;
			Transform skel = skeleton.GlobalTransform;
			
			Transform t=skel.AffineInverse() * b_target;

			skeleton.SetBoneGlobalPoseOverride(bone_IDs[skeleton.GetBoneName(i)], t, 1.0f, true);
		}
	}

	Transform get_bone_transform(int bone, bool convert_to_world_space = true)
	{
		Transform ret = skeleton.GetBoneGlobalPose(bone);

		if (convert_to_world_space)
		{
			ret = skeleton.GlobalTransform * ret;
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
