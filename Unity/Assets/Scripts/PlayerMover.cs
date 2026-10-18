using UnityEngine;

namespace Reborn1985.M1
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private M1Settings settings;
        [SerializeField] private Transform facingMarker;
        private Rigidbody2D body;
        private Vector2 input;
        private Vector2? verificationInput;
        public Vector2 Facing { get; private set; } = Vector2.down;
        public Rigidbody2D Body => body != null ? body : (body = GetComponent<Rigidbody2D>());

        public void Configure(M1Settings data, Transform marker)
        {
            settings = data; facingMarker = marker;
            if (marker != null) marker.localRotation = Quaternion.Euler(0, 0, 180);
        }
        private void Awake() { body = GetComponent<Rigidbody2D>(); }

        private void Update()
        {
            input = new Vector2(
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
        }

        private void FixedUpdate()
        {
            Vector2 direction = Vector2.ClampMagnitude(verificationInput ?? input, 1f);
            Body.linearVelocity = direction * settings.MovementSpeed;
            if (direction.sqrMagnitude > .001f)
            {
                Facing = direction.normalized;
                if (facingMarker != null)
                    facingMarker.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f);
            }
        }

        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; input = Vector2.zero; }

        // Only called by editor/development smoke verification, never by a gameplay UI.
        public void SetVerificationInput(Vector2? value) { verificationInput = value; }
        public void RestorePose(Vector2 position, Vector2 facing)
        {
            input=Vector2.zero; verificationInput=null; Body.linearVelocity=Vector2.zero;
            Body.position=position; transform.position=new Vector3(position.x,position.y,transform.position.z);
            Facing=facing.normalized;
            if (facingMarker!=null) facingMarker.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(Facing.y,Facing.x)*Mathf.Rad2Deg-90f);
        }
    }
}
