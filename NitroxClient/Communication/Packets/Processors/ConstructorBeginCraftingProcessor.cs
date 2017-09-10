﻿using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic.Helper;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using NitroxModel.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static NitroxClient.GameLogic.Helper.TransientLocalObjectManager;

namespace NitroxClient.Communication.Packets.Processors
{
    public class ConstructorBeginCraftingProcessor : ClientPacketProcessor<ConstructorBeginCrafting>
    {
        public static GameObject ConstructedObject;

        public override void Process(ConstructorBeginCrafting packet)
        {
            Optional<GameObject> opGameObject = GuidHelper.GetObjectFrom(packet.ConstructorGuid);

            if(opGameObject.IsEmpty())
            {
                Console.WriteLine("Trying to build " + packet.TechType + " with unmanaged constructor - ignoring.");
                return;
            }

            GameObject gameObject = opGameObject.Get();
            Crafter crafter = gameObject.GetComponentInChildren<Crafter>(true);

            if(crafter == null)
            {
                Console.WriteLine("Trying to build " + packet.TechType + " but we did not have a corresponding constructorInput - how did that happen?");
                return;
            }
                        
            MethodInfo onCraftingBegin = typeof(Crafter).GetMethod("OnCraftingBegin", BindingFlags.NonPublic | BindingFlags.Instance);
            Validate.NotNull(onCraftingBegin);
            onCraftingBegin.Invoke(crafter, new object[] { packet.TechType, packet.Duration }); //TODO: take into account latency for duration   

            Optional<object> opConstructedObject = TransientLocalObjectManager.Get(TransientObjectType.CONSTRUCTOR_INPUT_CRAFTED_GAMEOBJECT);

            if(opConstructedObject.IsPresent())
            {
                GameObject constructedObject = (GameObject)opConstructedObject.Get();
                GuidHelper.SetNewGuid(constructedObject, packet.ConstructedItemGuid);

                SetInteractiveChildrenGuids(constructedObject, packet.InteractiveChildIdentifiers);
            }
            else
            {
                Console.WriteLine("Could not find constructed object!");
            }
        }

        private void SetInteractiveChildrenGuids(GameObject constructedObject, List<InteractiveChildObjectIdentifier> interactiveChildIdentifiers)
        {
            foreach(InteractiveChildObjectIdentifier childIdentifier in interactiveChildIdentifiers)
            {
                UnityEngine.Transform transform = constructedObject.transform.Find(childIdentifier.GameObjectNamePath);

                if(transform != null)
                {
                    GameObject gameObject = transform.gameObject;
                    GuidHelper.SetNewGuid(gameObject, childIdentifier.Guid);
                }
                else
                {
                    Console.WriteLine("Error GUID tagging interactive child due to not finding it: " + childIdentifier.GameObjectNamePath);
                }
            }
        }
    }
}
