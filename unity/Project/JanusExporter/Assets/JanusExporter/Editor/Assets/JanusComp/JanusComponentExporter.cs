using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class JanusComponentExtractor
    {
        private JanusRoom room;
        public JanusComponentExtractor(JanusRoom room)
        {
            this.room = room;
        }

        public bool CanExport(Component[] comps)
        {
            return !comps.Any(c => c is JanusVREntryPortal || c is JanusVRLink);
        }

        public void Process(Component[] comps)
        {
            float uniformScale = room.UniformScale;

            for (int i = 0; i < comps.Length; i++)
            {
                Component comp = comps[i];
                if (!comp)
                {
                    continue;
                }

                if (comp is JanusVREntryPortal)
                {
                    JanusVREntryPortal portal = (JanusVREntryPortal)comp;
                    Transform portalTransform = portal.transform;

                    Vector3 portalPos = JanusUtil.ConvertPosition(portal.GetJanusPosition(), uniformScale);
                    Vector3 xDir, yDir, zDir;

                    Quaternion rot = portalTransform.rotation;
                    //rot.eulerAngles += new Vector3(0, 180, 0);
                    JanusUtil.GetJanusVectors(rot, out xDir, out yDir, out zDir);

                    room.PortalPos = portalPos;
                    room.PortalXDir = xDir;
                    room.PortalYDir = yDir;
                    room.PortalZDir = zDir;
                }
                else if (comp is JanusVRLink)
                {
                    JanusVRLink link = (JanusVRLink)comp;

                    Transform trans = link.transform;
                    Vector3 pos = JanusUtil.ConvertPosition(link.GetJanusPosition(), uniformScale);
                    Vector3 sca = trans.localScale;
                    Vector3 xDir, yDir, zDir;
                    JanusUtil.GetJanusVectors(trans.rotation, out xDir, out yDir, out zDir);

                    LinkObject linkObj = new LinkObject();
                    linkObj.pos = pos;
                    linkObj.xDir = xDir;
                    linkObj.yDir = yDir;
                    linkObj.zDir = zDir;
                    linkObj.col = link.Color;
                    linkObj.scale = sca;
                    linkObj.url = link.url;
                    linkObj.title = link.title;
                    //linkObj.image_id = link.url;

                    Material mat = link.meshRenderer.sharedMaterial;
                    Texture tex = mat.mainTexture;
                    if (tex != null)
                    {
                    }

                    room.AddLinkObject(linkObj);
                }
            }
        }

        public void ProcessNewRoomObject(RoomObject rObj, Component[] comps)
        {
            for (int i = 0; i < comps.Length; i++)
            {
                Component comp = comps[i];
                if (comp is MeshCollider)
                {
                    rObj.collision_id = rObj.id;
                }
                else if (comp is BoxCollider)
                {

                }
                else if (comp is SphereCollider)
                {

                }
                else if (comp is CapsuleCollider)
                {

                }
            }
        }
    }
}
