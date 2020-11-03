using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEditor.SearchService;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEditor;
using UnityEngine.PlayerLoop;

[AlwaysUpdateSystem]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public class SubSceneLoader : SystemBase
{
    private SceneSystem sceneSystem;
    private EndFramePhysicsSystem endPhysicsSystems;
    
    private Entity sceneBeingLoaded;
    private Entity sceneBeingUnloaded;
    
    private int currentSceneIndex;
    private bool isInitialSceneLoaded = false;
    protected override void OnCreate()
    {
        endPhysicsSystems = World.GetOrCreateSystem<EndFramePhysicsSystem>();
        sceneSystem = World.GetOrCreateSystem<SceneSystem>();
        currentSceneIndex = 0;
    }

    protected override void OnUpdate()
    {
        if (!isInitialSceneLoaded)
        {
            LoadScene(SubSceneReferences.Instance.subScenes[currentSceneIndex]);
            isInitialSceneLoaded = true;
            return;
        }
        #region CheckInputAndSetNewSceneIndex

        if (SubSceneReferences.Instance == null)
        {
            return;
        }

        int subScenesLength = SubSceneReferences.Instance.subScenes.Length;
        
        int keyQDown = Input.GetKeyDown(KeyCode.Q) ? subScenesLength - 1 : 0;
        int keyEDown = Input.GetKeyDown(KeyCode.E) ? 1 : 0;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadScene(SubSceneReferences.Instance.subScenes[currentSceneIndex]);
        }

        if (keyEDown + keyQDown == 0)
        {
            return;
        }

        int newSceneIndex = (currentSceneIndex + keyEDown + keyQDown) % subScenesLength;

        #endregion CheckInputAndSetNewSceneIndex

        if (newSceneIndex != currentSceneIndex)
        {
            //DependencyCompletion region doesn't change anything. It was my impression that this should ensure 
            //that all the physics-systems are safe to unload (it isn't async unload after all)
            #region dependencyCompletion

            var dep = endPhysicsSystems.GetOutputDependency();
            dep.Complete();

            if (!dep.IsCompleted)
            {
                Debug.Log("NOTCOMPLETED");
            }
            
            #endregion dependencyCompletion

            LoadScene(SubSceneReferences.Instance.subScenes[newSceneIndex]);
            UnloadScene(SubSceneReferences.Instance.subScenes[currentSceneIndex]);

            currentSceneIndex = newSceneIndex;
        }
    }

    private void LoadScene(SubScene subScene)
    {
        sceneSystem.LoadSceneAsync(subScene.SceneGUID);
    }

    private void UnloadScene(SubScene subScene)
    {
        sceneSystem.UnloadScene(subScene.SceneGUID);
    }
}