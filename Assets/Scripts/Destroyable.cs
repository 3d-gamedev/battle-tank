// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE text in the project root for license information.

using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace GameDev {
    /// <summary>
    /// destroyable script.
    /// </summary>
    public class Destroyable : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] int ENDURANCE = 3; // 耐久値

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        bool _auto_destroy = false; // 自動消去フラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives]

        public bool AutoDestroy { set => _auto_destroy = value; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        void Start() {
            /// <summary>
            /// 弾としてヒットしたとき
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x =>
                    gameObject.name.Contains(value: "Bullet") && // 自分の名前に "Bullet" が含まれる かつ
                    !x.gameObject.name.Contains(value: "Ground")) // 名前に "Ground" が含まれないオブジェクトに衝突したとき
                .Subscribe(onNext: x => {
                    Rigidbody rb = x.gameObject.GetComponent<Rigidbody>();
                    if (rb != null) {
                        rb.velocity = Vector3.zero; // ヒットした相手を一時停止
                    }
                    Destroy(obj: gameObject); // 自分を削除
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 弾がヒットしたとき
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.gameObject.name.Contains(value: "Bullet")) // 名前に "Bullet" が含まれるオブジェクトが衝突したとき
                .Subscribe(onNext: x => {
                    ENDURANCE--; // 耐久度を減らす
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 耐久度がゼロになったとき
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    ENDURANCE == 0 && // 耐久度がゼロ かつ
                    !name.Contains(value: "_Piece")) // 名前に "_Piece" が含まれない場合には
                .Subscribe(onNext: _ => {
                    explodePiece(); // 破片を生成する
                    ENDURANCE = -1; // 耐久度を無効化
                    Destroy(obj: gameObject); // 自分を削除
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 自動消去フラグがONになったとき
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _auto_destroy == true && // 自動消去フラグがON かつ
                    name.Contains(value: "_Piece")) // 名前に "_Piece" が含まれる破片である場合には
                .Subscribe(onNext: _ => {
                    // 破片を消去する
                    Observable
                        .TimerFrame(dueTimeFrameCount: Mathf.FloorToInt(f: Random.Range(minInclusive: 2500, maxInclusive: 5000)))
                        .Subscribe(onNext: _ => {
                            Destroy(obj: gameObject); // 自分(破片)を削除
                        }).AddTo(gameObjectComponent: this);
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// 破片を生成します
        /// </summary>
        void explodePiece(int number = 12, float scale = 0.5f, int force = 5) {
            // 複数の破片を作成
            for (int i = 0; i < number; i++) {
                // 破片オブジェクトを作成
                GameObject piece = Instantiate(original: gameObject); // 自分を複製して破片とする
                piece.name += $"_Piece_{i + 1}"; // 破片には名前に "_Piece_n" を付加する
                piece.transform.localScale = new Vector3(x: scale, y: scale, z: scale); // 縮小する
                if (piece.GetComponent<Rigidbody>() == null) {
                    piece.AddComponent<Rigidbody>(); // Rigidbody コンポーネントを持たなければ追加する
                }
                piece.GetComponent<Rigidbody>().isKinematic = false;
                piece.GetComponent<Rigidbody>().mass = 1.0f;
                // ランダム値のベクトルを取得
                Vector3 random_force = new Vector3(
                    x: Random.Range(minInclusive: force / 2, maxInclusive: force * 2), 
                    y: Random.Range(minInclusive: force / 2, maxInclusive: force * 2), 
                    z: Random.Range(minInclusive: force / 2, maxInclusive: force * 2)
                );
                // 破片に力と回転を加える
                piece.GetComponent<Rigidbody>().AddForce(force: random_force, mode: ForceMode.Impulse);
                piece.GetComponent<Rigidbody>().AddTorque(torque: random_force, mode: ForceMode.Impulse);
                // 破片の子オブジェクトを削除
                piece.GetComponentsInChildren<Transform>().ToList().ForEach(x => {
                    if (piece.name != x.name) { // 破片も破片の子リストにいるので除外
                        x.parent = null;
                        Destroy(obj: x.gameObject); // 破片の子オブジェクトは最初に削除
                    }
                });
                // 破片の自動消去フラグをON
                piece.GetComponent<Destroyable>().AutoDestroy = true;
            }
        }
    }
}