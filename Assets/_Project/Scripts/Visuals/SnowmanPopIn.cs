using UnityEngine;

namespace FreezeOrThrow.Visuals
{
    public class SnowmanPopIn : MonoBehaviour
    {
        [SerializeField] Transform[] bodyPieces;
        [SerializeField] Transform[] headPieces;
        [SerializeField] Transform[] detailPieces;
        [SerializeField] float pieceDuration = 0.42f;
        [SerializeField] float headDelay = 0.14f;
        [SerializeField] float detailDelay = 0.32f;
        [SerializeField] float detailStagger = 0.05f;
        [SerializeField] float overshoot = 1.9f;

        struct PoppingPiece
        {
            public Transform piece;
            public Vector3 restPosition;
            public Vector3 restScale;
            public Vector3 pivot;
            public float delay;
        }

        PoppingPiece[] poppingPieces;
        float elapsed;
        bool finished;

        void Awake()
        {
            Vector3 bodyCentre = bodyPieces.Length > 0 ? bodyPieces[0].localPosition : Vector3.zero;
            Vector3 headCentre = headPieces.Length > 0 ? headPieces[0].localPosition : bodyCentre;
            Vector3 groundPivot = new Vector3(bodyCentre.x, 0f, bodyCentre.z);
            Vector3 neckPivot = Vector3.Lerp(bodyCentre, headCentre, 0.6f);

            poppingPieces = new PoppingPiece[bodyPieces.Length + headPieces.Length + detailPieces.Length];
            int pieceIndex = 0;
            foreach (Transform piece in bodyPieces)
            {
                poppingPieces[pieceIndex++] = CreatePiece(piece, groundPivot, 0f);
            }
            foreach (Transform piece in headPieces)
            {
                poppingPieces[pieceIndex++] = CreatePiece(piece, neckPivot, headDelay);
            }
            for (int detailIndex = 0; detailIndex < detailPieces.Length; detailIndex++)
            {
                Transform piece = detailPieces[detailIndex];
                Vector3 anchorCentre = piece.localPosition.y > neckPivot.y ? headCentre : bodyCentre;
                Vector3 pivot = Vector3.Lerp(piece.localPosition, anchorCentre, 0.5f);
                poppingPieces[pieceIndex++] = CreatePiece(piece, pivot, detailDelay + detailStagger * detailIndex);
            }
        }

        void OnEnable()
        {
            elapsed = 0f;
            finished = false;
            ApplyProgress();
        }

        void OnDisable()
        {
            foreach (PoppingPiece poppingPiece in poppingPieces)
            {
                poppingPiece.piece.localPosition = poppingPiece.restPosition;
                poppingPiece.piece.localScale = poppingPiece.restScale;
            }
        }

        void Update()
        {
            if (finished)
            {
                return;
            }
            elapsed += Time.deltaTime;
            ApplyProgress();
        }

        static PoppingPiece CreatePiece(Transform piece, Vector3 pivot, float delay)
        {
            return new PoppingPiece
            {
                piece = piece,
                restPosition = piece.localPosition,
                restScale = piece.localScale,
                pivot = pivot,
                delay = delay,
            };
        }

        void ApplyProgress()
        {
            bool allSettled = true;
            foreach (PoppingPiece poppingPiece in poppingPieces)
            {
                float progress = Mathf.Clamp01((elapsed - poppingPiece.delay) / pieceDuration);
                allSettled &= progress >= 1f;
                float size = progress >= 1f ? 1f : Mathf.Max(0.0001f, PopCurve(progress));
                poppingPiece.piece.localScale = poppingPiece.restScale * size;
                poppingPiece.piece.localPosition = poppingPiece.pivot + (poppingPiece.restPosition - poppingPiece.pivot) * size;
            }
            finished = allSettled;
        }

        float PopCurve(float progress)
        {
            float remaining = progress - 1f;
            return 1f + (overshoot + 1f) * remaining * remaining * remaining + overshoot * remaining * remaining;
        }
    }
}
