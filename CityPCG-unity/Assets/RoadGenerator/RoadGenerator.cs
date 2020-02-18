using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    public int SEGMENT_COUNT_LIMIT = 256;
    public int HIGHWAY_SEGMENT_LENGTH = 25;

    [Range(0, 0.4f)]
    public float generationTickInterval = 0.2f;

    private class RoadSegment
    {
        public Vector3 start;
        public Vector3 end;
        public int t;
    }

    private List<RoadSegment> segments;
    private List<RoadSegment> segmentCandidates;
    private bool isGenerating = false;

    private Bounds cameraBounds;

    void Start()
    {
        segments = new List<RoadSegment>();
        segmentCandidates = new List<RoadSegment>();
    }

    RoadSegment ContinueStraight(RoadSegment previous, float angleDeviation)
    {
        Quaternion rotation = Quaternion.Euler(0, angleDeviation, 0);
        Vector3 dir = (previous.end - previous.start).normalized;
        Vector3 newDir = rotation * dir;

        return new RoadSegment {
            start = previous.end,
            end = previous.end + newDir * HIGHWAY_SEGMENT_LENGTH,
            t = previous.t + 1
        };
    }


    IEnumerator GenerateRoad()
    {
        Debug.Log("Started generating Road");
        isGenerating = true;

        segments.Clear();
        segmentCandidates.Clear();

        segmentCandidates.Add(new RoadSegment {start = new Vector3(0, 0, 0), end = new Vector3(50, 0, 0), t = 0});
        segmentCandidates.Add(new RoadSegment {start = new Vector3(0, 0, 0), end = new Vector3(-50, 0, 0), t = 0});

        cameraBounds = new Bounds(Vector3.zero, new Vector3(100, 1, 100));

        while (segmentCandidates.Count > 0 && segments.Count < SEGMENT_COUNT_LIMIT)
        {
            int min_t_index = 0;
            int min_t = segmentCandidates[min_t_index].t;

            // @optimization: get segment with smallest t using a priority queue
            for (int i = 0; i < segmentCandidates.Count; i++)
            {
                RoadSegment s = segmentCandidates[i];
                if (s.t < min_t)
                {
                    min_t = s.t;
                    min_t_index = i;
                }
            }

            RoadSegment candidate = segmentCandidates[min_t_index];
            segmentCandidates.RemoveAt(min_t_index);

            bool acceptedCandidate = true;
            {

            }

            if (acceptedCandidate)
            {
                segments.Add(candidate);
                cameraBounds.Encapsulate(candidate.end);

                // GlobalGoals
                {
                    segmentCandidates.Add(ContinueStraight(candidate, Random.Range(-10f, 10f)));

                    if (Random.value < 0.3f)
                    {
                        float branchAngle = 90 + Random.Range(-10.0f, 10.0f);
                        branchAngle = (Random.value) < 0.5 ? branchAngle : -branchAngle;
                        Debug.Log("Creating branch witch angle: " + branchAngle);

                        segmentCandidates.Add(ContinueStraight(candidate, branchAngle));
                    }
                }
            }

            yield return new WaitForSeconds(generationTickInterval);
        }

        Debug.Log("Finished generating road");
        isGenerating = false;
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (isGenerating)
            {
                StopCoroutine("GenerateRoad");
            }

            StartCoroutine("GenerateRoad");
        }

        foreach (RoadSegment s in segments)
        {
            Debug.DrawLine(s.start, s.end, new Color(0, 1, 0));
        }

        foreach (RoadSegment s in segmentCandidates)
        {
            Debug.DrawLine(s.start, s.end, new Color(1, 0, 0));
        }

        if (isGenerating)
        {
            Vector3 targetCameraPosition = cameraBounds.center + Vector3.up * (Mathf.Max(cameraBounds.size.x, cameraBounds.size.z) * 0.8f);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCameraPosition, 0.1f);
        }
    }
}
