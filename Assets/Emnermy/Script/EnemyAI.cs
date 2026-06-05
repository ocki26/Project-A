using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))] // Bắt buộc phải có Animator
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chasing, Attacking, Dead } // Thêm trạng thái Dead
    public enum RoamShape { Circle, Rectangle }

    [Header("Trạng thái hiện tại")]
    public State currentState = State.Patrol;

    public enum AIType { Slime, Skeleton, Mage }
    [Header("Đặc tính AI (Thêm mới)")]
    public AIType aiType = AIType.Slime;
    [Tooltip("Trạng thái bị choáng/khống chế của quái")]
    public bool isStunned = false;


    [Header("Chỉ số Chiến đấu & Sinh tồn")]
    public int maxHealth = 100;
    private int currentHealth;
    public int CurrentHealth => currentHealth; // Thuộc tính để Script Thanh máu đọc

    public float chaseSpeed = 3.5f;
    public int damage = 10;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;

    [Header("Chỉ số Phòng thủ (Thêm mới)")]
    public float defense = 5f;        // Giáp vật lý
    public float magicResist = 3f;    // Kháng phép


    [Header("--- TÙY CHỈNH VÙNG ĐI DẠO ---")]
    public RoamShape roamAreaShape = RoamShape.Rectangle;
    public Vector2 roamCenterOffset = Vector2.zero;
    public float roamRadius = 4f;
    public Vector2 roamRectSize = new Vector2(8f, 6f);
    
    [Space]
    public float patrolSpeed = 1.5f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("Chỉ số Tầm nhìn (Sensor)")]
    public float detectionRange = 7f;   
    [Range(0, 360)] public float viewAngle = 90f;       
    public float hearingRange = 2f;     

    [Header("Hệ thống Tránh Tường")]
    public LayerMask obstacleLayer;
    public float avoidDistance = 1.5f; 
    [Range(10, 90)] public float avoidAngle = 30f; 

    // --- Biến nội bộ ---
    private Transform player;
    private Rigidbody2D rb;
    private Animator anim; // Biến Animator
    
    private float lastAttackTime = 0f;
    private Vector2 movementDir;
    private Vector2 facingDirection = Vector2.down; // Mặc định nhìn xuống
    
    private Vector2 startPosition;
    private Vector2 roamDestination;
    private float roamTimer;

    // --- Các biến đẩy lùi (Knockback) ---
    private float knockbackTimer = 0f;
    private Vector2 knockbackVelocity;

    // --- Các biến phòng và phân thân ---
    private Collider2D currentRoomCollider;
    [HideInInspector]
    public bool hasSplit = false;

    public void SetRoomCollider(Collider2D roomCol)
    {
        currentRoomCollider = roomCol;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // Lấy component Animator
        
        currentHealth = maxHealth;
        startPosition = transform.position; 

        // Tự động sửa lỗi méo/kéo giãn tỷ lệ của quái vật ở Runtime (Phòng tránh trường hợp Prefab hoặc Spawner bị scale sai lệch)
        if (aiType == AIType.Slime)
        {
            // Chỉ đặt scale mặc định cho quái mẹ, tránh ghi đè làm biến dạng kích thước nhỏ của quái con phân tách
            if (!hasSplit)
            {
                transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }
        }
        else
        {
            float s = Mathf.Abs(transform.localScale.y);
            if (s > 0f)
            {
                transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * s, s, 1f);
            }
        }

        // Tự động tạo và gán Physics Material 2D không ma sát để tránh dính cứng vào tường vật lý
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null)
        {
            PhysicsMaterial2D frictionlessProto = new PhysicsMaterial2D("Frictionless_LPC");
            frictionlessProto.friction = 0f;
            frictionlessProto.bounciness = 0f;
            myCol.sharedMaterial = frictionlessProto;
        }

        // Tự động tìm kiếm căn phòng chứa quái vật hiện tại (Chỉ tìm nếu chưa được gán trước từ spawner)
        if (currentRoomCollider == null)
        {
            FindCurrentRoom();
        }

        PickNewRoamDestination();

        // Tự động gắn Thanh máu động lên quái vật khi khởi tạo!
        if (GetComponent<LPC_EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<LPC_EnemyHealthBar>();
        }

        // Cố gắng tìm Player lúc mới sinh ra
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void FindCurrentRoom()
    {
        RoomBounds[] rooms = FindObjectsOfType<RoomBounds>();
        foreach (var room in rooms)
        {
            Collider2D col = room.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(transform.position))
            {
                currentRoomCollider = col;
                return;
            }
        }

        RoomTrigger[] triggers = FindObjectsOfType<RoomTrigger>();
        foreach (var trig in triggers)
        {
            if (trig.roomBounds != null && trig.roomBounds.OverlapPoint(transform.position))
            {
                currentRoomCollider = trig.roomBounds;
                return;
            }
        }
    }

    void Update()
    {
        if (currentState == State.Dead) return; // Chết thì không làm gì cả

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }

        // Kiểm tra trạng thái bị Choáng/Khống chế
        if (isStunned)
        {
            movementDir = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            UpdateAnimations();
            return;
        }

        // ========================================================
        // [ĐÃ SỬA] LIÊN TỤC TÌM PLAYER NẾU CHƯA THẤY
        // Hệ thống Dungeon tạo quái từ xa, nên lúc đầu có thể chưa thấy Player
        // ========================================================
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) 
            {
                player = playerObj.transform;
            }
            else 
            {
                return; // Nếu vẫn không thấy Player thì đứng chờ frame tiếp theo
            }
        }
        // ========================================================

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Cập nhật hướng nhìn liên tục để truyền vào Animator
        if (movementDir.sqrMagnitude > 0.1f)
            facingDirection = movementDir.normalized;

        switch (currentState)
        {
            case State.Patrol:
                if (CanSeeOrHearPlayer(distanceToPlayer) && IsPointInsideRoamArea(player.position))
                {
                    currentState = State.Chasing;
                    break;
                }

                if (Vector2.Distance(transform.position, roamDestination) < 0.5f || IsStuck())
                {
                    movementDir = Vector2.zero; 
                    roamTimer -= Time.deltaTime;
                    if (roamTimer <= 0) PickNewRoamDestination(); 
                }
                else
                {
                    movementDir = (roamDestination - (Vector2)transform.position).normalized;
                }
                break;

            case State.Chasing:
                float chaseAttackRange = (aiType == AIType.Mage) ? 4.5f : attackRange;
                if (distanceToPlayer <= chaseAttackRange)
                {
                    currentState = State.Attacking;
                    movementDir = Vector2.zero; 
                }
                else if (distanceToPlayer > detectionRange * 1.5f || !HasLineOfSight(distanceToPlayer) || !IsPointInsideRoamArea(player.position))
                {
                    currentState = State.Patrol;
                    PickNewRoamDestination();
                }
                else
                {
                    // ─── ĐẶC TÍNH DI CHUYỂN RIÊNG BIỆT CỦA TỪNG LOẠI AI ───
                    if (aiType == AIType.Mage)
                    {
                        // Mage: Giữ cự ly 4m. Nếu quá gần (< 3m) lùi lại.
                        if (distanceToPlayer < 3.0f)
                        {
                            movementDir = -(player.position - transform.position).normalized; // Chạy lùi né áp sát
                        }
                        else if (distanceToPlayer > 5.0f)
                        {
                            movementDir = (player.position - transform.position).normalized; // Tiến lại gần để ngắm bắn
                        }
                        else
                        {
                            movementDir = Vector2.zero; // Đứng yên ngắm bắn
                            currentState = State.Attacking;
                        }
                    }
                    else if (aiType == AIType.Skeleton && currentHealth <= maxHealth * 0.3f && Time.time < lastAttackTime + attackCooldown)
                    {
                        // Skeleton: Khi yếu máu (< 30%) và đang hồi đòn đánh, di chuyển xoay tiếp tuyến (circle) né đòn!
                        Vector2 dirToPlayer = (player.position - transform.position).normalized;
                        Vector2 tangentDir = new Vector2(-dirToPlayer.y, dirToPlayer.x);
                        movementDir = (tangentDir + (-dirToPlayer * 0.25f)).normalized; // Vừa xoay tròn vừa lùi nhẹ
                    }
                    else
                    {
                        // Slime và Skeleton bình thường: Lao thẳng mặt player
                        movementDir = (player.position - transform.position).normalized;
                    }
                }
                break;

            case State.Attacking:
                float actualAttackRange = (aiType == AIType.Mage) ? 5.0f : attackRange;
                if (distanceToPlayer > actualAttackRange)
                {
                    currentState = State.Chasing;
                }
                else
                {
                    // Mage khi bị áp sát quá gần thì chuyển lại chasing để lùi lại
                    if (aiType == AIType.Mage && distanceToPlayer < 3.0f)
                    {
                        currentState = State.Chasing;
                        break;
                    }

                    movementDir = Vector2.zero; 
                    facingDirection = (player.position - transform.position).normalized;

                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        PerformAttack();
                    }
                }
                break;
        }

        UpdateAnimations(); // Gọi hàm cập nhật Animation
    }

    void FixedUpdate()
    {
        // [ĐÃ SỬA] Thêm chặn lỗi nếu chưa tìm thấy Player
        if (currentState == State.Dead || player == null || isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // --- Xử lý Đẩy lùi (Knockback) trượt mượt mà ---
        if (knockbackTimer > 0f)
        {
            rb.linearVelocity = knockbackVelocity;
            // Giảm dần lực đẩy lùi để tạo hiệu ứng ma sát trượt dừng lại mượt mà
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
            return; // Tạm thời bỏ qua tự động di chuyển của AI
        }

        float currentSpeed = (currentState == State.Patrol) ? patrolSpeed : chaseSpeed;

        // Áp dụng làm chậm do Freeze (nếu có)
        LPC_BuffManager buffMgr = GetComponent<LPC_BuffManager>();
        if (buffMgr != null && buffMgr.HasBuff(LPC_BuffManager.BuffType.Freeze))
        {
            currentSpeed *= 0.5f; // Freeze làm chậm 50%
        }

        if (movementDir != Vector2.zero)
        {
            Vector2 safeDir = CalculateObstacleAvoidance(movementDir);
            movementDir = Vector2.Lerp(movementDir, safeDir, Time.fixedDeltaTime * 10f).normalized;
        }

        rb.linearVelocity = movementDir * currentSpeed;
    }

    // ==========================================
    // HỆ THỐNG ANIMATION & COMBAT
    // ==========================================
    
    void UpdateAnimations()
    {
        // Gửi hướng X, Y vào Animator để biết đang nhìn hướng nào
        anim.SetFloat("moveX", facingDirection.x);
        anim.SetFloat("moveY", facingDirection.y);

        // Tính tốc độ hiện tại để biết là đang Đi (Walk), Chạy (Run) hay Đứng (Idle)
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // Gửi biến tốc độ và trạng thái rượt đuổi
        anim.SetBool("isMoving", currentSpeed > 0.1f);
        anim.SetBool("isChasing", currentState == State.Chasing);
    }

    void PerformAttack()
    {
        lastAttackTime = Time.time;
        // Kích hoạt animation đánh
        anim.SetTrigger("attack");
        
        if (aiType == AIType.Slime)
        {
            StartCoroutine(SlimeJumpDashCoroutine());
        }
        else if (aiType == AIType.Mage)
        {
            ShootMageSpell();
        }
        else
        {
            // Skeleton / Cận chiến thường: Trừ máu có độ trễ vung kiếm (0.3 giây)
            StartCoroutine(DelayedMeleeDamageCoroutine(0.3f));
        }
    }

    private System.Collections.IEnumerator SlimeJumpDashCoroutine()
    {
        if (player == null) yield break;
        Vector2 dashDir = (player.position - transform.position).normalized;
        float elapsed = 0f;
        float duration = 0.35f;
        float dashSpeed = chaseSpeed * 2.2f;

        Debug.Log("[AI Slime] Nhảy chồm kích hoạt!");

        while (elapsed < duration && currentState != State.Dead)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Kiểm tra gây sát thương thực tế lên Player ở cuối cú nhảy
        if (player != null && currentState != State.Dead)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.5f)
            {
                player.GetComponent<LPCPlayerController2>()?.TakeDamage(damage);
                Debug.Log($"[Combat] Slime nhảy trúng Player! Gây {damage} sát thương.");
            }
        }
    }

    private void ShootMageSpell()
    {
        if (player == null) return;
        Debug.Log("[AI Mage] Niệm phép tầm xa!");
        StartCoroutine(DelayedMageSpellCoroutine(0.5f));
    }

    private System.Collections.IEnumerator DelayedMageSpellCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player != null && currentState != State.Dead)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= 6.0f)
            {
                // Pháp sư bắn cầu phép gây sát thương phép bỏ qua giáp vật lý của Player
                player.GetComponent<LPCPlayerController2>()?.TakeDamage(damage, true);
                Debug.Log($"[Combat] Cầu phép của Mage bắn trúng Player! Gây {damage} sát thương phép.");
            }
        }
    }

    private System.Collections.IEnumerator DelayedMeleeDamageCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player != null && currentState != State.Dead)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                player.GetComponent<LPCPlayerController2>()?.TakeDamage(damage);
                Debug.Log($"[Combat] Skeleton chém trúng Player! Gây {damage} sát thương.");
            }
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
    {
        if (currentState == State.Dead) return;
        knockbackTimer = duration;
        knockbackVelocity = direction.normalized * force;
    }

    // --- HỆ THỐNG HITSTOP (KHỰNG HÌNH ĐẦM TAY) ---
    private bool isHitStopping = false;

    public void TriggerHitStop(float duration)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitStopCoroutine(duration));
        }
    }

    private System.Collections.IEnumerator HitStopCoroutine(float duration)
    {
        if (isHitStopping) yield break;
        isHitStopping = true;

        float originalSpeed = anim != null ? anim.speed : 1f;
        if (anim != null) anim.speed = 0.02f; // Khựng hình quái vật gần như hoàn toàn
        
        yield return new WaitForSeconds(duration);

        isHitStopping = false;
        if (anim != null) anim.speed = originalSpeed;
    }

    // Hàm gọi khi quái bị đánh (gọi từ script vũ khí của Player hoặc từ skill/đạn bay)
    public void TakeDamage(float amount, bool isMagicDamage = false, float penetration = 0f, bool triggerHurtAnim = true)
    {
        if (currentState == State.Dead) return;

        float finalDef = isMagicDamage ? magicResist : defense;
        // Trừ đi xuyên giáp/xuyên kháng
        finalDef = Mathf.Max(0f, finalDef - penetration);

        float calculatedDmg = Mathf.Max(1f, amount - finalDef);
        int damageAmount = Mathf.RoundToInt(calculatedDmg);

        currentHealth -= damageAmount;
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else if (triggerHurtAnim)
        {
            anim.SetTrigger("hurt"); // Bị thương chỉ kích hoạt khi không phải tick damage DOT!
        }
    }

    // Giữ overload cũ để tương thích ngược với các hàm gọi cũ nếu có
    public void TakeDamage(int damageAmount)
    {
        TakeDamage((float)damageAmount, false, 0f);
    }

    void Die()
    {
        currentState = State.Dead;
        rb.linearVelocity = Vector2.zero;
        
        // Reset các parameters di chuyển để tránh can nhiễu Animator
        anim.SetBool("isMoving", false);
        anim.SetBool("isChasing", false);
        anim.SetBool("isDead", true); // Kích hoạt chết, Animator tự chuyển sang Blend Tree Death mới cực chuẩn!
        
        GetComponent<Collider2D>().enabled = false; // Tắt va chạm
        
        // Tự động tính toán độ dài hoạt ảnh chết để hủy quái vật ngay khi kết thúc
        StartCoroutine(DestroyAfterAnimationCoroutine());
        
        // KHÔNG dùng this.enabled = false ở đây vì việc vô hiệu hóa component sẽ làm tạm ngưng (pause) toàn bộ Coroutine đang chạy trên component này.
        // Update() và FixedUpdate() của chúng ta đã có kiểm tra currentState == State.Dead ở đầu nên cực kỳ an toàn.
        Debug.Log("Enemy Died!");
    }

    private System.Collections.IEnumerator DestroyAfterAnimationCoroutine()
    {
        // Chờ 2 frame để chắc chắn Animator đã chuyển hẳn sang trạng thái die
        yield return null;
        yield return null;
        
        float animLength = 2.0f; // Mặc định dự phòng 2 giây
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // Lấy độ dài hoạt ảnh thực tế
            if (stateInfo.length > 0.1f)
            {
                animLength = stateInfo.length;
            }
        }
        
        // Đảm bảo hoạt ảnh chết diễn ra đầy đủ, không bị biến mất quá đột ngột
        animLength = Mathf.Max(1.2f, animLength);
        
        yield return new WaitForSeconds(animLength);

        // --- CƠ CHẾ SIÊU MẠNH: Slime Mẹ Phân Thân Khi Hoạt Ảnh Chết Đã Chạy Xong (Tỉ lệ 50%) ---
        if (aiType == AIType.Slime && !hasSplit && Random.value <= 0.50f)
        {
            hasSplit = true;
            SpawnMiniSlimes();
        }

        // Bỏ chặn an toàn: Hủy trực tiếp GameObject
        Destroy(gameObject);
    }

    // ==========================================
    // CÁC HÀM CŨ (GIỮ NGUYÊN)
    // ==========================================
    Vector2 GetRoamCenter() { return Application.isPlaying ? startPosition + roamCenterOffset : (Vector2)transform.position + roamCenterOffset; }
    
    bool IsPointInsideRoamArea(Vector2 point)
    {
        if (currentRoomCollider != null)
        {
            return currentRoomCollider.OverlapPoint(point);
        }
        Vector2 center = GetRoamCenter();
        if (roamAreaShape == RoamShape.Circle) return Vector2.Distance(center, point) <= roamRadius;
        else { float halfX = roamRectSize.x / 2f; float halfY = roamRectSize.y / 2f; return (point.x >= center.x - halfX && point.x <= center.x + halfX && point.y >= center.y - halfY && point.y <= center.y + halfY); }
    }

    void PickNewRoamDestination()
    {
        if (currentRoomCollider != null)
        {
            Bounds bounds = currentRoomCollider.bounds;
            float minX = bounds.min.x + 0.5f;
            float maxX = bounds.max.x - 0.5f;
            float minY = bounds.min.y + 0.5f;
            float maxY = bounds.max.y - 0.5f;

            for (int i = 0; i < 30; i++)
            {
                Vector2 randomPoint = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                if (currentRoomCollider.OverlapPoint(randomPoint) && !Physics2D.OverlapPoint(randomPoint, obstacleLayer))
                {
                    roamDestination = randomPoint;
                    roamTimer = Random.Range(waitTimeMin, waitTimeMax);
                    return;
                }
            }
        }

        Vector2 center = GetRoamCenter();
        for (int i = 0; i < 15; i++)
        {
            Vector2 randomPoint = (roamAreaShape == RoamShape.Circle) ? center + Random.insideUnitCircle * roamRadius : center + new Vector2(Random.Range(-roamRectSize.x / 2f, roamRectSize.x / 2f), Random.Range(-roamRectSize.y / 2f, roamRectSize.y / 2f));
            if (!Physics2D.OverlapPoint(randomPoint, obstacleLayer))
            {
                roamDestination = randomPoint;
                roamTimer = Random.Range(waitTimeMin, waitTimeMax);
                return;
            }
        }
        roamDestination = transform.position;
        roamTimer = waitTimeMin;
    }

    Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
    {
        // Hệ thống 5 tia quét (Multi-ray CircleCast steering) cho cảm giác né tường cực mượt
        float[] angles = { 0f, avoidAngle, -avoidAngle, avoidAngle * 1.8f, -avoidAngle * 1.8f };
        Vector2 bestDir = desiredDirection;
        float maxFreeDist = 0f;

        // Bán kính quét bằng kích thước thực tế của quái Slime để CircleCast chuẩn xác
        float castRadius = 0.25f;

        foreach (float angle in angles)
        {
            Vector2 rayDir = Quaternion.Euler(0, 0, angle) * desiredDirection;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, castRadius, rayDir, avoidDistance, obstacleLayer);
            
            if (hit.collider == null)
            {
                // Tia này hoàn toàn thông thoáng! Trả về hướng này ngay để có đường đi tối ưu nhất
                return rayDir.normalized;
            }
            else
            {
                // Nếu các hướng đều có chướng ngại vật, chọn tia có khoảng cách va chạm xa nhất (thông thoáng nhất)
                if (hit.distance > maxFreeDist)
                {
                    maxFreeDist = hit.distance;
                    bestDir = rayDir;
                }
            }
        }
        return bestDir.normalized;
    }

    bool IsStuck() { return (currentState == State.Patrol && rb.linearVelocity.sqrMagnitude < 0.01f && movementDir.sqrMagnitude > 0); }
    
    bool CanSeeOrHearPlayer(float distance)
    {
        if (distance <= hearingRange) return HasLineOfSight(distance);
        if (distance <= detectionRange) { Vector2 dirToPlayer = (player.position - transform.position).normalized; if (Vector2.Angle(facingDirection, dirToPlayer) < viewAngle / 2f) return HasLineOfSight(distance); }
        return false;
    }

    bool HasLineOfSight(float distance) { RaycastHit2D hit = Physics2D.Raycast(transform.position, (player.position - transform.position).normalized, distance, obstacleLayer); return hit.collider == null; }
     
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // 1. Vẽ vùng đi dạo (Màu xanh lá)
        Vector2 center = Application.isPlaying ? startPosition + roamCenterOffset : (Vector2)transform.position + roamCenterOffset;
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        if (roamAreaShape == RoamShape.Circle)
        {
            Gizmos.DrawWireSphere(center, roamRadius);
        }
        else
        {
            Gizmos.DrawWireCube(center, roamRectSize);
        }

        // Vẽ tâm của vùng đi dạo
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(center, 0.2f);
        
        // Vẽ đường nối tới điểm quái đang định đi tới (chỉ hiện khi Play game)
        if (Application.isPlaying && currentState == State.Patrol)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, roamDestination);
            Gizmos.DrawWireSphere(roamDestination, 0.2f); // Vẽ cục tròn nhỏ tại điểm đến
        }

        // 2. Râu kiến tránh tường (Xanh dương)
        Gizmos.color = Color.cyan;
        Vector2 faceDir = Application.isPlaying ? facingDirection : Vector2.down;
        Vector3 leftWhisk = Quaternion.Euler(0, 0, avoidAngle) * faceDir;
        Vector3 rightWhisk = Quaternion.Euler(0, 0, -avoidAngle) * faceDir;
        Gizmos.DrawLine(pos, pos + leftWhisk * avoidDistance);
        Gizmos.DrawLine(pos, pos + rightWhisk * avoidDistance);

        // 3. Tầm nhìn & Nghe & Đánh
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); 
        Gizmos.DrawWireSphere(pos, hearingRange); // Vùng nghe thấy (Xám)

        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(pos, attackRange);  // Tầm đánh (Đỏ)

        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(pos, detectionRange); // Tầm nhìn xa nhất (Vàng)
        
        // Vẽ 2 cạnh của góc nhìn (Góc chữ V)
        Vector3 rightViewAngle = Quaternion.Euler(0, 0, viewAngle / 2f) * faceDir;
        Vector3 leftViewAngle = Quaternion.Euler(0, 0, -viewAngle / 2f) * faceDir;
        Gizmos.DrawLine(pos, pos + rightViewAngle * detectionRange);
        Gizmos.DrawLine(pos, pos + leftViewAngle * detectionRange);
    }

    private void SpawnMiniSlimes()
    {
        int spawnCount = 2;
        Debug.LogWarning($"[AI Slime] Slime Mẹ vỡ vụn! Phân tách thành {spawnCount} Slime Con!");

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 offset = (i == 0) ? Vector3.left * 0.4f : Vector3.right * 0.4f;
            GameObject child = Instantiate(gameObject, transform.position + offset, Quaternion.identity);
            child.SetActive(true);

            // Bật lại các component thiết yếu trên bản sao (vì quái mẹ vừa tắt chúng khi gọi Die)
            var childAI = child.GetComponent<EnemyAI>();
            var childCol = child.GetComponent<Collider2D>();
            
            if (childCol != null) childCol.enabled = true;
            
            if (childAI != null)
            {
                childAI.enabled = true;
                childAI.currentState = State.Patrol;
                childAI.hasSplit = true; // Chặn đệ quy vô hạn: Slime con không phân thân nữa
                
                // Tăng sức mạnh tương đối: Máu = 50% mẹ, Damage = 60% mẹ, Tốc độ = 1.3 lần mẹ
                childAI.maxHealth = Mathf.Max(20, maxHealth / 2);
                childAI.currentHealth = childAI.maxHealth;
                childAI.damage = Mathf.Max(3, Mathf.RoundToInt(damage * 0.6f));
                childAI.chaseSpeed = chaseSpeed * 1.3f;
                childAI.patrolSpeed = patrolSpeed * 1.3f;

                // Reset hoàn toàn Animator của con để không bị kẹt ở trạng thái nằm chết thừa hưởng từ mẹ
                var childAnim = child.GetComponent<Animator>();
                if (childAnim != null)
                {
                    childAnim.Rebind(); // Reset toàn bộ state machine, parameters về mặc định ban đầu
                    childAnim.SetBool("isDead", false);
                }

                // Thu nhỏ kích thước bản sao xuống 60%
                child.transform.localScale = transform.localScale * 0.6f;

                // Tái thiết lập điểm tuần tra tươi mới
                childAI.startPosition = child.transform.position;
                childAI.FindCurrentRoom();
                childAI.PickNewRoamDestination();
            }
        }

        // Tạo chữ nổi màu lục "MINI SLIMES!" nổi lên báo hiệu
        GameObject splitText = new GameObject("Runtime_SplitText");
        splitText.transform.position = transform.position + Vector3.up * 1.0f;
        TMPro.TextMeshPro tmp = splitText.AddComponent<TMPro.TextMeshPro>();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 4f;
        LPC_FloatingText ft = splitText.AddComponent<LPC_FloatingText>();
        ft.Setup("MINI SLIMES!", new Color(0.2f, 0.85f, 0.2f), 1.2f);
    }
}