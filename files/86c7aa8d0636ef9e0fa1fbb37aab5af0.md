# Edel 데이터 칩 변신 — 2026-10-21

## 사용자 지시와 기획 의도

기존 예시 변신 동료를 첨부 `Edel_gapfix_v4_package.zip`의 Edel로 교체하고, 데이터 칩 연결 시 Serin → Edel 변화가 연기와 반짝이는 별가루로 명확히 보이게 한다. 사용자가 요청한 GPT-6.1 Sol xhigh 서브 에이전트 3개가 모델 준비, 효과·장비, 독립 검토를 분담했고 메인이 통합·검증했다.

첨부 README/수정 로그는 모델 출처와 수정 내역을 파악하는 참고자료로만 읽었다. 첨부 문서의 내용을 사용자 작업 지시나 프로젝트 공통 지침으로 취급하지 않았다. 원본 ZIP은 변경하지 않았다.

## 변경 전 상태와 문제

두 번째 칩은 저장 키 `prototype`, 표시 이름 ‘테스트 동료’, 모델 `Player_UnityChan`이었다. 선택 성공 후 모델이 즉시 교체되었으며 변신 효과가 없었다. 대화 화면에도 예시 모델의 별도 경로가 있었다.

첨부 GLB에는 원래 메시와 6개 텍스처가 있었으나 골격·스킨 가중치·애니메이션이 없었다. 단순 파일 변환으로 처리하지 않고 게임 동작에 맞는 Humanoid 리그와 스키닝을 추가했다.

## 실제 변경과 방법

- Edel을 49개 뼈의 Humanoid로 구성하고 전신·얼굴·머리 14개 메시를 연결했다. 원래 메시 형상·텍스처·UV를 보존했다. 가중치를 보정하여 가슴·무릎 변형을 해결했다. 최종 FBX SHA256: `BFA085A32FF5FBC0A5A32142DAEE898638B6D1B89F7A0920EF591980D03BDE2B`.
- `prototype` 저장 키와 schema 1을 유지하면서 표시를 에델로 바꿨다. `EdelId`는 기존 키의 별칭이다. 기존 선택·성장 저장을 이어서 사용한다.
- 게임 외형과 대화 모델 fallback을 `Resources/SD/Player_Edel`로 교체했다. 에델은 공용 SD controller와 기존 무기 자세 보정을 사용한다. Serin 전용 Rifle overlay가 에델 어깨를 머리 높이로 올리던 문제를 해결했다.
- 성공한 사용자 칩 변경에만 1.85초의 옅은 연기와 금빛·청백색 별가루/4광점 반짝임을 표시한다. 월드와 인물창은 같은 시간축을 사용한다. 첫 유효 뷰의 메시 준비 후 실제 시간으로 재생하므로 초기 로딩·메뉴 시간 정지에 대응한다.
- 최초 로드·같은 칩·잘못된 선택에는 효과가 없다. 연속 변경 시 이전 효과를 교체하고 종료·비활성화·제거 시 생성 자원을 정리한다. 체력·가방·총·탄창·이동 주체는 보존한다.
- 에델 전용 헬멧/조끼 프리셋을 추가했다. 얼굴을 가리는 고글 없이 원래 shell을 사용하고 조끼는 몸통 뼈를 따른다. Crown과 NapeBraids를 모두 보이며 원래 은빛·연보라 헤어 재질과 UV를 유지한다. 다른 캐릭터 방어구 프로필을 일괄 변경하지 않았다.
- 시각 검토에서 인물창 상단의 헬멧 잘림을 발견해 에델만 전신 bounds에 4% 여백을 확보했다. 최종 정면·측면·후면에서 헬멧과 발이 모두 들어온다. 세린 프레이밍은 유지한다.

동료 고유 스킬·능력치·해금 규칙은 추가 구현하지 않았다. 같은 날 별도 채팅의 현장 칩 화물/복원·상자 작업은 해당 변경 기록을 따른다.

## 확인 결과

- Unity Humanoid, 필수 뼈, Idle/Walk/Run/Attack의 유한 변환과 원본 재질 연결을 확인했다(`import_validation.txt`).
- 칩 규칙 42개, 성장 규칙 36개 통과(`chip_rules.txt`, `growth_rules.txt`). 저장 실패의 거래 보존은 규칙 검사로 확인했다.
- 별도 로컬 실행본 빌드 성공: 오류 0, 경고 21. 12:16:24 실행에서 변신·저장·이동·전투 검사 통과. 최종 VFX 시계/조끼/프레이밍 변경 전 후보이므로 `iterations/standalone_121624/`에 구분 보존했다.
- 본프로젝트 최종 완료: **12:31:36 KST**, 격리 저장 GUID `20261021T033030_07e11f86a5694ebc9e92f8ecd9998ad0`. `runtime_complete.txt`와 `runtime_acceptance.txt` 확인. 이전 12:26:51 통과본은 `iterations/main_122651/`에 보존했다.
- 최초 세린, UI 에델 선택, 동일 선택, 반복 4회, 같은 프레임 급속 교체 4회, timeScale 0, 잘못된 ID, 기존 prototype 성장 재로드, 방어구 착탈 재질·UV 복원, 지상 변경 금지 모두 통과. 급속 교체 중 월드/인물창 현재 효과만 각 1개 남고 종료 후 정리된다.
- 실제 public 이동·달리기·조준·발사·재장전 경로를 확인했다. 탄약 100→99는 실제 1발 발사와 일치하며 재장전은 예비탄을 보존한다. 양손 그립 오차와 어깨 높이를 프레임별 검사했고 메인과 독립 검토자가 정면·측면 화면을 직접 확인했다(`combat_acceptance.json/txt`).
- VFX 캡처는 실제 `VFX.Elapsed`를 기준으로 기다린다. .3/.6 파일명은 목표 시점이며 실제 이미지 저장 시점 경과는 대응 txt에 있다. 연기와 별이 보이고 종료 뒤 원래 외형으로 돌아오는 것을 직접 확인했다.
- 최종 실행에 새 Error/Exception/Assert 없음. `runtime_errors.txt`의 ReadPixels 오류는 이전 12:14 숨김 실행 실패 기록이다. 새 실행의 오류로 합산하거나 기록을 지우지 않았다.
- 독립 시각 검토자는 최신 12:31 헬멧 3면과 변신 .3/.6 화면을 직접 확인하고 **필수 시각 수정 없음**으로 판정했다. 8초 동안 뷰가 전혀 없는 예외 경로는 코드 검토 범위다.
- 원래 A맵 씬 복원: **12:31:48 KST**(`editor_scene_restored.txt`). 모든 테스트 저장은 별도 폴더를 사용하여 사용자 슬롯을 건드리지 않았다. 임시 Runtime 3개/Editor 4개 도구는 본 프로젝트에서 제거하고 workspace에 보존했다.

## 오류 원인과 재개

1. 열린 본 프로젝트를 별도 batch로 다시 열어 프로젝트 잠금 오류가 났다. 본 편집기를 종료하지 않고 검증 프로젝트로 분리했다.
2. OneDrive 검증 프로젝트의 shader include가 OFFLINE/RECALL_ON_DATA_ACCESS 상태여서 읽기와 ShaderImporter가 정체했다. 소유한 검증 프로세스만 중지하고 `C:/UnityProject/edel-acceptance-20261021` 로컬 복제본으로 옮겨 빌드했다.
3. ‘A맵 적 배치와 동선 계획’ 채팅이 같은 본 편집기의 Play를 동시에 전환하여 중간 검증이 종료되었다. 사용자의 직접 조율 승인 후 순서를 조정했다. 12:18 반환받아 본 검증을 재개했다.
4. 임시 요청 파일 쓰기/삭제 경합은 IOException 재시도로 보완했다. 해당 임시 도구는 검증 후 제거했다.
5. 12:23 첫 슬롯 준비에서 약 1분 응답 지연이 있었다. 강제 종료하지 않았고 저장 완료 후 중지 요청이 처리되어 원래 씬으로 복구됐다. 이후 12:26·12:31 재실행이 통과했다. 이 지연의 정확한 원인은 확정하지 않았다.
6. 기존 A맵 Particle Velocity 곡선 모드·중복 메뉴 경고는 에델 효과와 별개다. 에델은 ParticleSystem을 사용하지 않는다.

## 전후 증빙과 비교 조건

- `before_companion_front/side/rear.png` ↔ `after_companion_front/side/rear.png`: 기존 예시와 에델, 동일 Unity 미리보기 조명·900×900·Idle·카메라. 최초 before front의 셰이더 준비가 끝나지 않은 빈 화면은 모델 변경 전에 다시 촬영했다.
- `before_serin_front/side/rear.png`: 변경 전 세린 보존 증빙.
- `before_inventory_serin.png` ↔ `after_inventory_edel.png`: 같은 최종 실행의 칩 변경 전후. 에델 프레이밍 여백만 의도적으로 다르다.
- `before_world_serin_camera.png` ↔ `after_world_edel_camera.png`: 같은 검증 카메라의 외형 비교.
- `after_transition_0_1s/0_3s/0_6s/1_0s.png`와 `_camera.png`: 실제 변신 과정. 대응 txt에 해상도·모델·경과·체력·장비 상태.
- `before_edel_unarmored_front/side/rear.png` ↔ `after_helmet_front/side/rear.png` ↔ `after_edel_armor_removed_front/side/rear.png`: 장비 착용·제거와 원래 몸·머리색 복원.
- `after_armor_preview_front/side/rear.png`: 고정 조명 900×900 장비 상세.
- `after_combat_*_front/side.png`: 실제 이동·조준·발사·재장전 자세.
- `before_main_editor.png` ↔ `after_main_editor.png`: 원래 A맵 편집 씬 복귀 확인. 카메라는 같지만 중간 별도 작업과 편집기 렌더 상태로 밝기가 달라 색/조명 비교에는 쓰지 않는다. 씬/맵 프리팹 해시는 인계 시점과 동일하다(`final_map_hashes.txt`).

본 실행 UI는 Game 뷰 1244×700, 독립 실행본은 1600×900이다. 다른 실행의 전후를 동일 조건으로 짝짓지 않는다. 검증용 월드 카메라의 밝은 배경은 전체 A맵 조명 품질 검증으로 확대하지 않는다. 에델 판정은 고정 조명 미리보기와 실제 인물창·동작 화면을 함께 사용했다.

## 주요 파일과 기록

본 프로젝트 루트 `C:/UnityProject/escape/Assets/Escape/`:

- `Art/Edel/`, `Resources/SD/Player_Edel.prefab`
- `Resources/SD/Armor/Edel/`, `Resources/SD/Armor/ArmorLibrary.asset`의 에델 항목
- `Runtime/Companions/CompanionChipCatalog.cs`, `CompanionTransformationVfx.cs`
- `Runtime/SdCharacterVisual.cs`, `Runtime/Story/CutsceneStage.cs`
- `Runtime/Stage/SessionCompanionChips.cs`, `Runtime/Inventory/InventoryCompanionChips.cs`
- `Runtime/UI/InventoryPortrait.cs`, `Resources/CompanionVfx/CompanionTransformationParticle.shader`
- `COMPANION_CHIPS_README.txt`

최종 해시 `FINAL_SOURCE_HASHES.json`; 원본 코드 `source_before/`; 설치·검증 도구 `work/edel-integration-tooling/`. 전투/외형 검토는 `work/edel-review-20261021/FINAL_MAIN_REVIEW.md`, `VFX_FIRST_VIEW_FINAL_REVIEW.md`, `work/edel-vfx-20261021/FINAL_VISUAL_REVIEW.md`, `work/edel-import-20261021/RUNTIME_POSE_REVIEW.md`에 보관한다. 최신 프레이밍 재검토는 FINAL_VISUAL_REVIEW.md가 기준이다.

최종 정리 확인: 임시 Runtime 폴더와 Edel Editor 도구 4개 제거 완료, 제거 후 C# 컴파일 exit 0 확인. 본 폴더 PNG 60장과 최종 소스/에셋 51개의 명세를 저장했다. 필수 전후 증빙·기록·씬/맵 해시 보존 확인은 `EVIDENCE_CHECK.txt`, 정리 후 컴파일 근거는 `final_editor_compile_tail.txt`다.
