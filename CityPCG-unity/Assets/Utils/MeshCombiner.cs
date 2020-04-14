using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//http://answers.unity.com/answers/1123487/view.html
//Credit: Bunzaga
public static class MeshCombiner {

     public static void Combine(GameObject o)
     {
         ArrayList materials = new ArrayList();
         ArrayList combineInstanceArrays = new ArrayList();
         MeshFilter[] meshFilters = o.GetComponentsInChildren<MeshFilter>();

         foreach (MeshFilter meshFilter in meshFilters)
         {
             MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

             if (!meshRenderer ||
                 !meshFilter.sharedMesh ||
                 meshRenderer.sharedMaterials.Length != meshFilter.sharedMesh.subMeshCount)
             {
                 continue;
             }

             for (int s = 0; s < meshFilter.sharedMesh.subMeshCount; s++)
             {
                 int materialArrayIndex = Contains(materials, meshRenderer.sharedMaterials[s].name);
                 if (materialArrayIndex == -1)
                 {
                     materials.Add(meshRenderer.sharedMaterials[s]);
                     materialArrayIndex = materials.Count - 1;
                 }
                 combineInstanceArrays.Add(new ArrayList());

                 CombineInstance combineInstance = new CombineInstance();
                 combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                 combineInstance.subMeshIndex = s;
                 combineInstance.mesh = meshFilter.sharedMesh;
                 (combineInstanceArrays[materialArrayIndex] as ArrayList).Add(combineInstance);
             }
         }

         // Get / Create mesh filter & renderer
         MeshFilter meshFilterCombine = o.GetComponent<MeshFilter>();
         if (meshFilterCombine == null)
         {
             meshFilterCombine = o.AddComponent<MeshFilter>();
         }
         MeshRenderer meshRendererCombine = o.GetComponent<MeshRenderer>();
         if (meshRendererCombine == null)
         {
             meshRendererCombine = o.AddComponent<MeshRenderer>();
         }

         // Combine by material index into per-material meshes
         // also, Create CombineInstance array for next step
         Mesh[] meshes = new Mesh[materials.Count];
         CombineInstance[] combineInstances = new CombineInstance[materials.Count];

         for (int m = 0; m < materials.Count; m++)
         {
             CombineInstance[] combineInstanceArray = ((ArrayList) combineInstanceArrays[m]).ToArray(typeof(CombineInstance)) as CombineInstance[];
             meshes[m] = new Mesh();
             meshes[m].CombineMeshes(combineInstanceArray, true, true);

             combineInstances[m] = new CombineInstance();
             combineInstances[m].mesh = meshes[m];
             combineInstances[m].subMeshIndex = 0;
         }

         // Combine into one
         meshFilterCombine.sharedMesh = new Mesh();
         meshFilterCombine.sharedMesh.CombineMeshes(combineInstances, false, false);

         // Destroy other meshes
         foreach (Mesh oldMesh in meshes)
         {
             oldMesh.Clear();
             Object.DestroyImmediate(oldMesh);
         }

         // Assign materials
         Material[] materialsArray = materials.ToArray(typeof(Material)) as Material[];
         meshRendererCombine.materials = materialsArray;

         foreach (MeshFilter meshFilter in meshFilters)
         {
             Object.DestroyImmediate(meshFilter.gameObject);
         }
     }

     private static int Contains(ArrayList searchList, string searchName)
     {
         for (int i = 0; i < searchList.Count; i++)
         {
             if (((Material)searchList[i]).name == searchName)
             {
                 return i;
             }
         }
         return -1;
     }
}
