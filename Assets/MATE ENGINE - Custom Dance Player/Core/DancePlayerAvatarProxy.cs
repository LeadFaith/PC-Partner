﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomDancePlayer
{
    public class DancePlayerAvatarProxy : MonoBehaviour
    {
        public DancePlayerCore playerCore;

        // Animation Event method - can be called from animation clips
        public void OnAnimationEnd()
        {
            Debug.Log("SceneAvatarProxy: OnAnimationEnd called");
            if (playerCore != null)
            {
                playerCore.PlayNext();
            }
        }

    }
}
