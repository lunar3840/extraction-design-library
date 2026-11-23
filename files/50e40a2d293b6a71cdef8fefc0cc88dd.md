# 전체 UI 공통 스타일 · Figma → Unity

## 사용자 지시와 의도

인벤토리에는 Figma 시안이 반영되었으나 파밍창/플레이 HUD/메인 등 나머지가 일치하지 않는다. 현재 인벤토리 디자인을 기준으로 모든 실제 UI를 Figma에서 제작·정리하고 Unity에 적용한다. 새 게임 기능을 추가하거나 최근 리그·배낭 정책을 되돌리는 작업이 아니다.

## 현재 조사 및 작업 순서

- 메인이 기획/Figma/공통 skin/통합 검증 담당. HUD와 Frontend 읽기 조사를 독립 병렬 수행했다.
- 현재 Unity는 Figma 인벤토리 탭의 footer 검증 → 캐릭터 탭의 portrait/idle 검증 순서로 사용 중이다. 명시 반환 전 production 및 Editor/Library 상태를 바꾸지 않는다. 준비는 workspace staging과 Figma로 한정한다.
- 기존 Figma file `KUD4t84BL1S77c3cnb8co0`, inventory `105:2987`, 최신footer `159:1039`, components `0:1`, Gameplay `11:711`, Basecamp `11:712` 확인. 기존 시안과 컴포넌트를 보존하고 새 revision을 만든다.

## 디자인 자산 발견 체크리스트

- [x] 원본 소스: Unity IMGUI, FigmaInventorySkin/Layout 및 UiTheme를 읽었다. Noto Sans KR Regular/Bold(기존 Medium도 허용), 기본1920×1080→logical1600×900.
- [x] 필요한 요소: Panel/Button/Pill/Keycap/TierItem/Slot/Tooltip/Status/Interaction/Progress/Toast/Ability와 화면별 composition.
- [x] Code Connect 탐색: 현재 Escape 소스에 *.figma.* 없음. C# IMGUI 수동 매핑 사용.
- [x] 기존 화면/컴포넌트: kit 및 Gameplay/Basecamp/inventory 페이지를 읽고 실제 component key/variant/property 확인. 해당 local 요소 재사용 가능.
- [x] 변수: Primitive 및 Extraction Dark Glass의 실제 로컬 semantic/spacing/radius 확인. panel#647080/44%, tile white6%, mint#72EBD1, borderwhite16%, text#F5F7FB.
- [x] 라이브러리/검색: get_libraries 및 지정된 Panel/Button/panel/spacing/Body 검색. remote 검색 결과로 local kit 부재를 추론하지 않음. 이미 local components/tokens/styles가 정확한 기준이므로 외부 Material/iOS 키트로 교체하지 않음.
- [x] 텍스트/효과 스타일: Extraction/Display/Title/Heading/Body/Caption/Readable/HUD, Glass Regular/Dense와 Depth 확인. 실제 사용 가능 font스타일 목록 확인.
- [x] 대상은 비웹 Unity이므로 web capture 워크플로 해당 없음. Figma의 플레이어·총·지도는 예시 데이터이며 Unity에서는 실제 동적 모델/아이템/지도 소스를 유지한다. 전체UI 스크린샷을 구현용 이미지로 쓰지 않는다.

## 변경 전 확인

아직 production 수정 전이다. 실제 before 캡처는 Unity 인계 후 확보한다. Figma 원본은 보존하며 get_design_context의 실제 render를 사전 참고로 확인했다. 이 문서는 진행기록이며 완료보고가 아니다.

## 보존할 기능

리그12칸/장착배낭 용량, V리그전용 자동치료/취소, 더블클릭 상자전송 우선, 장착/해제/사용, 드래그/회전/맞교환/버리기, 최고티어 자동장전 및 수동R, 파밍 목록 안정순서·휠·F단발, 머리위 HP/stamina 분리fill, 캐릭터 외형/고정pose/드래그 회전, 실제 지도 이미지·방향·상호작용을 보존한다.

## 검증 주의

기존 의료휠 검사는 최신 no-op 계약과 충돌한다. 이를 통과시키려고 삭제된 기능을 복구하지 않는다. ChestRigBackpack/더블클릭/자동장전과 화면별 최신 route로 검사한다. 완료 전 전후이미지·Figma editability·font/asset·실제 Unity 컴파일/동작/해상도검증 및 주요파일 목록을 추가한다.

## Figma 제작 진행

- 원본을 보존한 revision 페이지: `178:559`, `2026-10-23 · Unified UI · Inventory Glass`.
- 편집 가능한 21개 화면: 파밍/리그·배낭, 플레이 HUD, 메인, 기지 출격 로비, 출격 준비, 저장 슬롯, 확인·로딩·오류, 보관함, 상점, 공용 연구, 동료 능력, 회수 칩, 의뢰·기록, 기지 이동 HUD, 전술 지도, 무기 개조·탄약, 동료 칩, 대화·기록, 탈출·귀환, 사망·시간 만료, 아이템 상세.
- Noto Sans KR/기존 semantic color·spacing·radius 및 로컬 Button/Panel/Nav/Inventory/Interaction 컴포넌트를 재사용했다. 전체 UI를 한 장의 래스터로 씌우지 않는다.
- 메인은 실제 게임의 두 줄 제목·태그라인·소개·저장 동작을 유지하는 좌표로 시안을 정리했다. 샘플 캐릭터는 실제 Unity portrait로 대체할 콘텐츠 자리이다.
- HUD의 미구현 compass/목표 카운터/고정 탈출거리/점선 탄창 샘플은 revision에서 숨겼다. 실제 미니맵·시계·두 무기·리그 치료·E/X 좌표에 맞췄다.
- 다운로드한 Figma 캡처는 `before_figma_*`, `after_figma_*`다. 실제 게임 변경 전후 증빙을 대신하지 않는다. `MenuAtmosphere.png`만 UI를 포함하지 않은 단독 배경 에셋으로 사용할 예정이다.

## 적용 준비 및 독립 검증

- staging: 공통 FigmaGameSkin, UiTheme 최소 패치, HUD4/Frontend5/파밍·기지·보관함3 및 SessionGlass 최소 패치를 준비했다. 아직 Assets에 적용하지 않았다.
- 캐릭터/푸터 채팅이 같은 파일을 계속 수정하므로 최신 소스에 context patch만 합친다. prepared 파일 전체 복사로 덮어쓰지 않는다.
- Unity bundled Roslyn 독립 runtime 컴파일: 원본/통합후보/common 추가후보 모두 exit0·error0. 기존과 동일한 경고21개. `staging/frontend/compile/COMPILE_RESULT.md` 참조. 실제 Unity import/Play 성공을 뜻하지 않는다.
- 삭제된 휠 대신 실제 리그 추천·치료·상태를 포함한 37화면 독립 캡처 helper를 준비했고 정적 컴파일을 통과했다. 사용자 저장은 읽거나 쓰지 않고 Library UUID fixture만 쓴다. 아직 실제 route를 실행하지 않았다.
- V 추천과 상태 토스트의 기존 겹침을 발견해 스타일 패치에 위치 보정을 포함했다. rect 조합 검사는 통과했으나 실제 화면 검증은 남아 있다.

## 공유 편집기 대기 상태

캐릭터 채팅에서 사용자 추가 확대 요청을 검증 중이다. 그 반환 후 인벤토리 푸터 채팅이 전용 수정·촬영 슬롯을 사용하고, 이어 이 작업이 Unity를 인계받아 실제 before 촬영 → 최소 패치 적용 → after 촬영·회귀검증을 수행한다. 명시 반환 전에 편집기/Assets/Library를 변경하지 않는다.

## 인계 후 실제 확인 및 저장 공간 중단

- 인벤토리 푸터 채팅의 명시 반환 후 Unity를 인계받았다. v08 / Edit / dirtyFalse / FullHD1920 상태와 helper `home_status`를 확인했다. 최신 footer26icon/30current 및 캐릭터 inventory1.06/lobby1.16·발끝 정렬을 보존한다.
- `before_editor_handoff_visible.png`는 Unity를 전면에 둔 실제 시작 화면이다. 최초 `before_editor_handoff.png`는 다른 프로젝트 앱에 가려진 잘못된 캡처였다. 이 작업에서 생성한 해당 오캡처 한 장만 제거했으며 라이브러리 게시·비교 대상에서 제외한다. 올바른 시작 화면과 실제 경고 화면은 보존한다.
- production 추가는 `Assets/Escape/Editor/UnifiedUiEvidenceChecks.cs` 검증 helper 한 파일뿐이다. 독립 및 실제 Unity Editor 컴파일을 마쳤으며 helper는 idle, owned=false, pending request 없음이다. Runtime UI/C#·리소스·사용자 프로필·씬 에셋은 아직 변경하지 않았다.
- 실제 플레이 before/after route는 아직 시작하지 않았다. SystemsTest를 열거나 Play에 진입하지 않았고, 원래 v08 Edit 상태다.
- Unity가 "project drive diskspace below 1.00 GB" 경고를 표시했다. 바로 확인한 C드라이브 여유는1,315,504,128 bytes(약1.23GiB)로, 컴파일 임시파일 정리 후 잠깐 늘어난 것으로 보인다. `before_blocked_disk_warning.png`에 실제 경고를 저장했다.
- 37개 화면의 전후 저장과 Unity import/Play/cache 작업 도중 공간이 더 줄어들 수 있어 Runtime 스타일 적용과 대량 검증을 시작하지 않았다. 다른 사용자 파일·다른 프로젝트·Unity 캐시를 임의 삭제하지 않았다.
- 재개 조건: C드라이브에 추가 여유 확보(최소5GB 이상 권장) 후 현재 원본 상태를 다시 확인하고 실제 before route부터 수행한다. 중단 상태를 적용 완료로 보고하지 않는다.
- 현재 Figma21화면과 workspace staging 패치/독립 컴파일 결과는 보존되어 있다. 실제 after 결과가 없으므로 Unity 적용 완료 조건은 충족하지 않았다.

## 2026-10-23 공간 확보 후 재개 준비

C드라이브 여유38,181,728,256bytes(약35.6GiB)를 다시 읽기 확인해 저용량 조건이 해소됐음을 확인했다. 이전 기록은 당시 중단 이력으로 보존한다. 인벤토리 채팅의 추가 footer 전용 적용·검증이 끝나 명시 반환될 때까지 Assets/Editor/Library를 조작하지 않고 최신 소스 대조와 기획 라이브러리 주제 연결을 준비한다.

library_ready: true
library_topic_id: topic:outputs/change_records/2026-10-22/ui-kit-completion-unity
library_category: UI·UX
library_subcategory: 공통 UI·도구
library_kind: 변경 결과
library_instruction_date: 2026-10-23
library_round_order: 2026-10-23-01
library_round_summary: 2026-10-23 지시 · 기존 인벤토리의 공통 글꼴·패널·버튼·민트 강조를 Figma21화면과 현재 Unity 전체 UI 경로로 확장. 실제37상태 전후와1920/1600 해상도 검수, 마지막 로비 결과배너를 포함한38상태 재검증, 제한된 실제 마우스 및 모델 회귀를 확인. 별도 캐릭터·푸터 변경과 단축키 미검증을 구별.
library_publish_status: published
library_published_index_hash: 25f426dacb5687b36fa94ce21e815d7983dbd2375f37821b278bf35718cbd507
library_publication_evidence: LIBRARY_PUBLICATION_VERIFICATION.json

기존 원문/회차·10/21 Figma 설계·10/23 인벤토리 기준 링크는 보존한다. 신규 폴더를 별도 대표로 만들지 않으며, 현재의 Figma 제작·패치 준비 상태를 구현 완료 회차로 세지 않는다. 주제 연결 검토 근거는 `staging/common/LIBRARY_TOPIC_REVIEW.md`에 있다.

## 실제 통합 진행 · 2026-10-23 KST

인벤토리 푸터 담당의 명시 반환(32px 현재값/15px 보조값/26px 아이콘, y896) 후 이 채팅이 Unity를 인수했다. `before_editor_resumed.jpg`에서 v08/Edit/FullHD 원상태를 확인했고, 일반 UI로 SystemsTest를 열고 Play에 진입했다. 사용자 저장은 불러오지 않았으며 Library UUID 전용 fixture만 사용했다.

- 원본 before37개와 최초 after37개가 실제 Game View1920×1080 PNG로 저장됐다. `before_manifest_1920x1080.txt`, `after_manifest_1920x1080.txt` 모두42단계 도달·캡처37·geometryFailures0·Title/noSession cleanup 완료다. API로 준비한 화면 증빙이며 native 입력 검증과 구별한다.
- 전후는 같은 시드20261023/해상도/화면 상태를 사용한다. 실시간 타이머·동료 pose·카메라 보간과 월드 오브젝트 프레임은 일부 다를 수 있으며, 월드 화면까지 픽셀 단위로 동일하다고 주장하지 않는다.
- 14개 기존 Runtime 소스를 최신 상태에 최소 context patch로 반영했다. 원본은 `production_before/`에 보존하고 SHA256을 기록했다. 후보 전체 파일 덮어쓰기는 하지 않았다.
- `FigmaGameSkin.cs` 추가, UiTheme/Frontend/HUD/지도/파밍/기지/보관함/성장/의뢰/대화/Session 표현을 기존 Inventory 토큰·폰트로 연결했다. 제목·메뉴 구성, 스크롤 가능한 저장 상태/오류, 실제 portrait 드래그·클릭은 기존 동작 API를 사용한다.
- UI가 없는 Figma 배경 원본 `MenuAtmosphere.png`만 Resources에 추가했다. 실사용 SHA256 `2D3D2976E56B29C6DCAC7B4ED77AB7832F403344C71A319792371613D5BC4EC5`. 실제 import1920×1080/sRGB/uncompressed/noMip/noNPOT/clamp/bilinear 검증은 `after_asset_import.txt`다. 전체 UI 이미지를 입력 위에 씌우지 않았다.
- 실제 Unity import/runtime+Editor 컴파일은 오류 없이 완료됐다. 기존 Particle Velocity/중복 메뉴 경고는 이번 변경 이전에도 있었다.
- 독립 검수에서 주요 텍스트 신규 잘림/겹침은 없었다. 상세34장과 HUD22장 비교는 `staging/common/ACTUAL_SCREEN_REVIEW.md`, `staging/hud/ACTUAL_SCREEN_REVIEW.md`에 있다. hover/입력은 사진만으로 통과 처리하지 않았다.
- 순수 모델 후속 검사 `staging/common/offline/post_apply_01/`에서 실제 적용 DLL 대상으로89개 assertion 및10000회 유한 이동 수량 보존을 통과했다. 이는 실제 UI 입력 검증의 대체가 아니다.

### 검수 중 보완

- 파밍 목록의 같은 상자 위에 Scout의 금색 월드 표식이 겹치던 기존 문제가 확인됐다. `LootInteractor.IsPromptVisibleFor`는 상태를 변경하지 않는 표시 전용 조회다. 실제 보이는 최대8개 행/현재 선택 구간/모든 표시 guard만 검사하며, 목록에 표시된 동일 상자의 중복 marker만 숨긴다. 후보 탐색·정렬·상호작용·수색·아이템 거래는 바꾸지 않았다.
- 일부 작은 캡슐과 원형 버튼의9-slice 중앙 높이가0이 되면서 생기는 얇은 이음선을 수정했다. 공유 담당의 명시 허용 후 `FigmaInventorySkin.Surface` corner 상한에서1logicalpx 여유만 확보했다. 폰트·박스·클릭 영역·푸터 좌표는 그대로다. 실제 재촬영 검증은 이어 진행한다.
- 인벤토리 담당의 별도 사용자 후속 지시 ‘조금 아래로’를 조율해 `FigmaInventoryVitals` Y896→912 두 곳과 해당 review bridge 기대값 한 곳만 통합했다.32/15px·26px아이콘·616×44·항목간격은 불변이다. Figma는 그 담당이 소유해 같은16px 하향을 반영했다. 이 변경은 별도 `inventory-vitals-lowered` 회차이며 전체 UI 지시 횟수에 더하지 않는다.

최종 after_r02 재촬영, native 입력, 작은 해상도 검증 및 원래 씬 복원은 아직 진행 중이다. 따라서 현재 `library_ready: false`와 게시 pending을 유지한다.

## 최종 적용·검증 결과 · 앞선 진행 상태 갱신

위 staging·디스크 중단·진행 중 문단은 각각 당시 이력이며 삭제하지 않는다. 아래 결과가 최신 상태다.

- 편집 가능한 Figma21화면을 실제 UI 경로에 적용했다. 인벤토리와 같은 Noto Sans KR, 어두운 반투명 패널, 얇은 경계, 민트 선택/주요 버튼, 공통 제목·본문·보조문구 비율을 사용한다. Figma revision은 [178:559](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0?node-id=178-559)다. 단독 배경을 제외한 패널·문구·버튼·슬롯은 조작 가능한 기존 UI이며 전체 화면 이미지를 씌우지 않았다.
- 같은37상태의 before1920↔after_r02 1920과 추가1600 37장, native7장의 실제 파일/PNG헤더/상태 로그를 확인했다. [전후 대응표](EVIDENCE_MANIFEST.md)에 각 원본을 연결했다. 독립 검수 결과는 [공통](staging/common/ACTUAL_SCREEN_REVIEW.md)·[HUD/파밍](staging/hud/ACTUAL_SCREEN_REVIEW.md)이다.
- 파밍 중복 표식과 짧은9-slice 버튼 이음선은 r02/작은 해상도 실제 재촬영 후 해소됐다. 로비 ResultSummary가 이름/동료 칩 선택을 가리는 기존 문제는 결과 안내만 우측 출격 패널의 버튼 위로 옮겨 해결했다. 사망→긴급 회수→로비의 실제 문자열 두 줄이1920/1600 모두 완전히 표시되고 CTA와 간격이 남음을 메인·독립 검수자가 확인했다.
- 로비 보완을 포함한 최종 재검증은 [after_r03 1920](after_r03_manifest_1920x1080.txt)·[after_r02 1600](after_r02_manifest_1600x900.txt) 각각43단계/38캡처/geometryFailures0/Title-noSession cleanup 완료다. 보완 전1600과 보완 후1600은 같은 실패 문구지만 무기 장착/손실 및 자세는 다르며 이 조건 차이를 비교표에 명시했다.
- 실제 마우스로 새 게임/슬롯/기지 진입, 로비↔기지 버튼, 장비 탭, 헬멧 더블클릭 장착, 탄약 드래그, Y912 체력 클릭을 관측했다. [입력 검증](NATIVE_INPUT_REVIEW.md)에 성공과 미검증을 구분했다. 자동 조작의 Tab/I로 게임 전환이 관측되지 않아 Tab/I/F/V/R native 성공을 주장하지 않는다. 물리 키보드에서 실패한다고 확정한 것도 아니며 입력 생산 코드는 이번 스타일 작업에서 바꾸지 않았다.
- 기존 의료 휠/숫자 소모품 퀵슬롯/옛 동료 선택을 되살리지 않았다. 실제 리그12칸/장착배낭, V 자동 치료와 기존 드래그·더블클릭·맞교환·드롭·장전·지도 기능을 보존한다. 기능 전부를 native로 재검증했다고 확대하지 않는다.
- 최신 캐릭터 담당의 catalog 기반 타이틀 로스터와 FrontEnd 최소3훅을 보존한 상태로 최종 route를 실행했다. 그 캐릭터 설계·검증과 푸터Y912 수정은 각 담당의 별도 회차이며 이 지시의 추가 횟수로 세지 않는다.
- 최종 생산 runtime DLL SHA256: `F95A61D863E7FA00030E829E12DA68BE32F2AD474BB5D3E56B71C7F30CB3B63C`. Editor DLL: `D241A90DF334F2AD4AA760E6BC1992E634BC4F2B51D2AEAC9F42D9654014FC62`. Unity가 새 소스를 컴파일하고 두 실제 경로를 완료했다. 기존 Particle Velocity/중복 menu 경고는 별도 기존 문제이며 이번에 해결했다고 보고하지 않는다.
- 명시 cleanup/Play 종료 뒤 원래 `StageA_Relay_Blockout_v08` Edit/dirtyFalse/FullHD1920×1080으로 정상 로드·Repaint를 확인했다. [최종 복원 화면](after_editor_final_return.jpg). helper active/owned/cleaning/interactive 모두false, pending request0, 저장 override 원복이다. 사용자 일반 프로필과 씬 에셋은 저장·덮어쓰기하지 않았다. 관련 캐릭터 채팅에 Unity 점유 해제를 명시 통보했다.

### 최종 DLL 모델 회귀 재확인

마지막 배너 보완 및 다른 담당 타이틀 병합 후의 실제 DLL로 [새 독립 검사](staging/common/offline/post_apply_final/FINAL_MODEL_REVIEW.md)를 실행했다. fresh compiler/runner exit0,89 assertions(24+17+10+38)와10,000회 수량200 보존을 통과했고, DLL·직접 규칙·검사 helper8개 입력 SHA256이 전후 동일했다. 현재 최종 DLL 해시와 일치한다. production/Editor/Library/기존 로그는 이 검사에서 변경하지 않았다. 순수 모델 결과이며 실제 UI/입력 전체 통과로 확대하지 않는다.

### 주요 수정 파일과 자산

Unity 루트는 `C:/UnityProject/escape/`다.

- 새 공통 표현: `Assets/Escape/Runtime/UI/FigmaGameSkin.cs`, 기존 `UI/UiTheme.cs`, `UI/FigmaInventorySkin.cs`의 작은 이음선 보정.
- 메인/슬롯: `UI/FrontEndController.cs`. HUD/의료: `Inventory/InventoryHudView.cs`. 실제 지도: `Mapping/TacticalMapView.cs`.
- 파밍·인벤토리·기지: `Inventory/InventoryGlassView.cs`, `Loot/LootInteractor.cs`, `Companions/CompanionAbilityController.cs`, `Stage/SessionInventoryPresentation.cs`, `Stage/SessionBaseCamp.cs`, `Stage/SessionGlassView.cs`.
- 연구/동료/의뢰/대화: `Stage/SessionGrowthView.cs`, `Stage/SessionChipRecoveryView.cs`, `Stage/SessionQuestView.cs`, `Story/CutsceneDirector.cs`.
- 단독 배경: `Assets/Escape/Resources/UI/FigmaGame/MenuAtmosphere.png`와 importer. 검증 전용: `Assets/Escape/Editor/UnifiedUiEvidenceChecks.cs`.
- 푸터 담당 요청으로 통합한 별도 회차 파일: `Runtime/UI/FigmaInventoryVitals.cs`, `Editor/InventoryVitalsReviewBridge.cs`.32/15px·26px아이콘·Y912을 보존.

14개 기존 runtime의 시작 전 원본과 해시는 `production_before/`에 있다. 다른 담당의 후속 변경 때문에 현재 전체 파일이 초기 staging과 완전히 같다고 주장하지 않는다. 기존 원본·중간 검증과 모든 원본 이미지는 이력으로 보존한다.

### 라이브러리 인계

사용자 원문은 [SOURCE_REQUEST.md](SOURCE_REQUEST.md)에 보존했다. 기존 주제 ID를 유지하고 `2026-10-23-01`은 당일 정렬키로만 사용한다. 이것을 사용자 지시 횟수 또는 누적 구현 횟수로 해석하지 않는다. 명시 대응은 `LIBRARY_EVIDENCE.json`의 실제 파일 허용목록·comparisons로 전달하며 파일 이름을 맞추려고 원본을 복제하지 않는다. 실제 게시·원문·전후 이미지 재조회가 확인되기 전에는 게시 완료로 처리하지 않는다.

도구 상태 증빙2장(before_editor_resumed/after_editor_final_return)은 Sky가 반환한 실제 JPEG였다. 최초 확장자.png를 바이트 변경 없이.jpg로 바로잡고 SHA256 동일을 확인했다. 허용목록의 나머지는 실제 Game View121장과 Figma4장의 PNG다. 이 확장자 교정은 게임 전후 사진을 변형·재촬영한 것이 아니다.
