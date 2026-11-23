# 전투 타격감 첫 묶음 — 2026-10-21

## 사용자 지시와 기획 의도

명중·머리 명중·처치 마커와 확인음, 기본 총성, 플레이어 피격 방향 표시와 충격음을 Unity에 적용하고 실제 플레이로 검증한다. 사용자가 지정한 **GPT-6.1 Sol medium** 서브 에이전트 3개가 HUD 제작, 음향 제작, 독립 검증을 분담했고 메인이 통합·실행·기록을 맡았다. 맵/배치 작업이 아니므로 과거 맵 전용 모델 설정을 확대 적용하지 않았다.

발사에 비해 명중 보상과 피격 인지가 빈약한 문제를 해결한다. 기존 피해·방어구·무기·AI 규칙에 새 피해나 경직을 넣지 않고 실제 결과를 화면과 음향으로 전달한다.

## 변경 전 상태

기존 RifleController에는 총구 발광·조준 반동·탄도 선과 명중 결과가 있지만 명중 확인 HUD/음향이 없었다. DamageReceiver는 피해·흡수량·부위·출처를 전달했으나 플레이어 순간 피격 호/충격음으로 연결되지 않았다. 기존 몸통 외상 비네트/흐림은 지속 부상 표현이었다.

수정 전에 `before_editor.png`와 `source_before/` 원본 7개를 저장했다. 변경 전 별도 실행본도 실제로 빌드·실행하여 모든 원래 피해 검사가 통과했고 오디오 출력은 0이었다. 최초 검증 전용 바닥이 기본 Standard 재질이라 URP에서 분홍색으로 보여 해당 실행의 원본 화면·로그는 `iterations/initial_magenta_fixture/`에 보존했다. 게임의 원래 맵 재질 오류로 확대하지 않는다.

최종 비교용 바닥에는 검증 도구에서만 URP/Lit를 명시했다. 그 후 **검증 사본에서 보관 원본 RifleController/EnemyController/EnemyController.MachineRoles 3개를 복원**, before 모드의 명시적 새 피드백 생성도 비활성화하고 변경 전 동작을 다시 실행했다. 최종 변경 전 그림은 모두 `before_reconstructed_*.png`로 표시했다. 사후 화면을 변경 전 화면으로 바꾸거나 원본 실행인 것처럼 표시하지 않았다. 본 프로젝트의 변경 사항을 되돌려 촬영하지는 않았다.

## 실제 변경과 방식

- RifleController의 테스트 초기화 및 실제 소유 무기 바인딩 경로에서 로컬 피드백 구성요소를 한 번 연결한다.
- 한 발의 모든 펠릿 처리 후 처치 > 머리 > 일반 명중 중 대표 결과 하나를 선택한다. 일반 명중은 양의 실제 체력 피해·Applied 상태·실제 대상이 있어야 한다. 빗나감·보호·죽은 표적·중복·무효 결과는 확인음을 만들지 않는다.
- 일반 X 0.12초, 머리 마름모/X 0.16초, 처치 코너가 달린 적색 X 0.22초. 검은 외곽선과 중앙 여백을 사용한다. 다음 발의 결과는 이전 처치 마커도 교체한다. 실제 입력에서는 반동이 적용된 현재 조준점을 따른다.
- 피해자가 공격자를 향하는 방향을 피격 순간에 저장하고 카메라 방향으로 투영한다. 체력 피해는 붉은 호, 완전 흡수는 청색 이중 호, 출처가 없으면 방향 없는 균형 가장자리 표시다. 최대 4개를 유지하고 가까운 방향은 합친다. 숨은 적의 위치를 계속 추적하지 않는다.
- 실제 플레이어 DamageReceiver 이벤트를 구독한다. 같은 프레임의 여러 펠릿은 충격음 한 번으로 묶는다. 치명적 마지막 피격은 0.18초간 보여준 뒤 정리한다.
- 가방 화면에서는 전투가 계속되므로 적 총성·피격 경고는 유지하고 발사/명중 마커만 억제한다. 실제 시간 정지·메뉴·입력 소유 전환은 잔여 효과를 정리한다.
- 인간 등 일반 원거리 NPC와 기계 hitscan 역할의 실제 발사 경로에 원거리 기본 총성을 연결한다. NPC 교전으로 플레이어 명중 확인음을 발생시키지 않는다. 유탄·로켓·방전 폭발용 음향은 이번 기본 총성 묶음의 범위가 아니다.
- 48kHz mono PCM WAV 36개(9그룹 × 변형 4개)를 원본 합성으로 제작했다. 일반/머리/처치, 권총/소총/산탄/저격, 신체/방어구 피격을 구별한다. PCM·미리 로드로 짧은 효과음을 준비한다.
- 확인 3/로컬 총성 4/원거리 총성 8/피격 3의 고정 AudioSource 18개를 재사용한다. 원거리 음량·팬은 플레이어 위치와 카메라 오른쪽을 기준으로 계산한다. 소음기 배율은 음향에 반영하되 AI 인지 신호는 그대로 사용한다. 전용 System.Random으로 게임 전역 난수에 영향을 주지 않는다.

기대 효과는 조준점·확인음으로 명중 결과를 즉시 읽고, 화면 중앙을 가리지 않는 방향 호와 충격음으로 피격을 알아채는 것이다. 이번에 새로운 적 경직·체력/탄약 밸런스·방어구 파괴 규칙·맵 배치·캐릭터 재질을 추가하지 않았다.

## 실제 확인 결과

### 최종 별도 실행본

`C:/UnityProject/combat-feedback-validation-20261021`는 별도 검증 프로젝트다. 본 프로젝트의 현재 Runtime과 음향을 복사하여 Windows 실행본을 빌드했다. 원본 검증은 보관 소스로 바꾼 이 사본에서만 수행했고, 최종 실행본은 설치된 최종 소스로 다시 빌드했다.

`after_build.log`: Unity 빌드 성공, return code 0.

`after_runtime.txt`: **48 PASS, 실패 0**. 실제 RifleController.FireDirection 및 물리 콜라이더로 빗나감·몸통·머리·처치·죽은 표적·보호 표적·8펠릿·몸통/머리/처치가 섞인 3펠릿을 검사했다. 처치 직후 다음 몸통 표시 교체, NPC 실제 TryAttack 발사와 원거리 총성/확인음 배제, 피해 중복 방지, 완전 흡수, 전후좌우와 카메라 90도 회전, 출처 없는 피해, 일시정지, 가방 중 피격, 반복 바인딩, 씬/플레이어 교체, 마지막 치명 피격과 종료를 확인했다.

카메라 회전 검사에서 월드 +Z 공격은 화면 왼쪽 -90.00001도로 투영되었다. 출처 없는 피해는 DirectionalCueCount 0, EnvironmentalCueCount 1을 확인했다(`directions=1` 로그는 총 피해 표시 수다).

`after_actual_output.wav`: 실제 AudioListener 출력 녹음. 956,416 오디오 프레임, 299,169 비영 샘플, 최대 진폭 **0.6284791**. 이 통제된 검사 녹음에서는 클리핑이 없었다. `before_reconstructed_actual_output.wav`는 같은 피드백이 없던 경로의 출력 0을 확인했다. 오디오 신호 검사는 사람의 주관적인 청감 평가를 대신하지 않으며 임의의 모든 동시 교전에 대한 마스터 믹스 보증도 아니다.

### 본 A맵

`main_map_runtime.txt`: **12 PASS, 실패 0**. 원래 열린 StageA_Relay_Blockout_v08 씬의 StageScenePreview를 실제 Play했다. 원래 제공되는 임시 StageMapPreview 저장 경로를 사용했다(로그에 별도 GUID 있음). 실제 소유 AR_001의 빈 탄창에서 재장전하여 임시 검증 표적에 몸통·머리·처치 발사를 수행하고 마커·확인음 및 플레이어 피격 호/충격음을 확인했다. 36클립 로드/누락 0, AudioSource 18, 컨트롤러 1개도 검사했다. 임시 표적은 Play 안에서만 생성하고 종료 전에 제거했으며 씬을 저장하지 않았다.

원래 씬으로 Play 종료한 경로가 로그에 기록됐다. 본 A맵의 기존 `Particle Velocity curves must all be in the same mode` 오류 반복은 이전 작업 기록에도 있는 상태이며 이번 피드백의 새 예외로 합산하지 않았다. 최초 기계 역할 enum using 누락과 임시 검증 도구 API 오타는 수정 후 최종 실행이 통과했다. 초기 실패 로그는 삭제하지 않고 보존했다.

### 직접 입력 및 독립 검토

최종 별도 실행본에서 실제 마우스 클릭으로 표적 HP **1000 → 990 (Body 10) → 970 (Head 20)**을 확인했다. `after_manual_before_fire/body_fire/head_fire.png`가 실제 창 기록이다. 자동 검사 전용 직접 FireDirection 호출과 이 실제 입력 확인을 구별한다.

HUD 담당자가 실제 본 A맵 4장과 최종 비교 화면을 이미지 도구로 직접 읽고 **필수 수정 없음**으로 판정했다. 기계적인 제작 완료 보고만으로 통과하지 않았다. `work/combat-feedback-20261021/hud/FINAL_VISUAL_REVIEW.md`. 코드 및 실행 결과 독립 검토는 `work/combat-feedback-20261021/validation/FINAL_REVIEW.md`를 참조한다.

본 Runtime 전체의 최종 C# 빌드도 성공했다(오류 0, 기존 경고 22). 기존 피해 계산/출처/히트스캔/플레이어 이동 코드는 백업과 해시가 동일하며 A맵 씬의 작업 전 해시 `C9E3D324A6DA7849188D1D92F9D79843B8E4820DCBE8325C3A0B227CD13D1874`도 최종 보존 여부를 별도 명세에서 확인한다.

## 전후 스크린샷 대응

- `before_editor.png` ↔ `after_editor.png`: 같은 본 편집기 씬·Scene 카메라. 기능 코드를 바꾼 것이므로 편집 맵 자체의 차이는 없다.
- `before_reconstructed_body/head/kill.png` ↔ `after_body/head/kill.png`: 같은 1280×720·카메라·표적·조명, 각 발사 직후.
- `before_reconstructed_damage_right/left/front/back/damage_rotated_camera/armor_impact/source_null.png` ↔ 같은 이름의 `after_*.png`: 해당 피격 시점. 실제 파일명은 `before_reconstructed_damage_rotated_camera.png`처럼 phase prefix에 시점명이 이어진다.
- `before_reconstructed_idle/miss/pellets/mixed_pellets/paused/inventory/inventory_incoming/rebind/scene_rebind/lethal_impact/lethal_cleared.png` ↔ 같은 시점의 `after_*.png`: 생명주기 및 중복·누락 검사.
- `after_main_map_body/head/kill/incoming.png`: 본 A맵 실제 최종 Play 화면(1244×700). 별도 검증본의 변경 전 그림과 동일 조건인 것처럼 짝짓지 않는다.
- `after_main_map_idle/loaded.png`: 초기 본 A맵 실행 확인. 이 화면만으로 명중 검증 완료를 주장하지 않는다.

최초 분홍색 검증 바닥 화면은 `iterations/initial_magenta_fixture/`에 보존했다. 재구성본과 최종본을 구별한다.

## 주요 파일 및 남은 범위

본 프로젝트 `C:/UnityProject/escape/Assets/Escape/`:

- 신규: Runtime/Combat/CombatFeedbackController.cs, CombatFeedbackHud.cs, CombatFeedbackAudio.cs
- 수정: Runtime/Weapon/RifleController.cs, Runtime/Enemy/EnemyController.cs, Runtime/Enemy/EnemyController.MachineRoles.cs
- 신규: Resources/CombatFeedbackAudio/ WAV 36개 및 import 설정, COMBAT_FEEDBACK_README.md

원본 합성기·수치 감사·검증 도구·독립 검토는 workspace `work/combat-feedback-20261021/`에 보존했다. 본 편집기의 임시 CombatFeedbackMainMapProbe는 완료 후 제거했다. 최종 파일 명세·증빙 존재·동일성 검사는 FINAL_FILE_HASHES.json, EVIDENCE_CHECK.json에 저장한다.

이 검사는 타격 피드백과 연결 경로의 범위다. 장시간 전투 청감, 다양한 기계 동시 교전의 음향 균형, 실제 장착 방어구 내구도 전체, NPC 자율 판단·맵 전체 AI를 새로 검증한 것으로 확대하지 않는다. 머리 마커의 작은 중앙 디테일은 이동 중 인지성에 대한 후속 플레이 튜닝 대상으로 남긴다.
