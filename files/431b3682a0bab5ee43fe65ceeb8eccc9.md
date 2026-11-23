---
library_topic_id: topic:outputs/change_records/2026-10-13/a-map-concept-rebuild-d09
library_category: 맵·레벨 디자인
library_subcategory: 건물·실내·외벽
library_kind: 구현 명세
library_instruction_date: 2026-10-29
library_round_order: 2026-10-29-05
library_round_summary: 여러 정적 환경 에셋을 개별·일괄 처리하는 실제 파손·파편·노후화·덩굴 통합 도구와 폐차·건물 비교 시안
library_ready: true
library_publish_status: published
library_publish_reason: 기존D09 당일05회차로 허용29문서·22이미지·7명시쌍 공개 게시 및 익명해시·실제화면 열람 확인
library_published_url: https://extraction-design-library-ehoji.ehojin0415.chatgpt.site/#outputs%2Fchange_records%2F2026-10-29%2Fa-map-structural-ruin-studies%2FCHANGE_RECORD.md
library_verified_at: 2026-10-29T13:08:20.190Z
library_publication_receipt: outputs/change_records/2026-10-29/a-map-structural-ruin-studies/PUBLICATION_VERIFICATION.json
---

# 실제 구조 파손과 풍성한 덩굴 시안

## 사용자 원문

> 노후화를 모델링으로 파편같은것도 좀 추가해주면서 더 디테일하게 해줬으면 좋겠어. 지금은 부서진 건물보단 때 탄 건물이라는 느낌이 좀 더 강한 것 같아. 건물이 파괴된 효과도 좀 더 추가해줘야 할 것 같고. 덩굴도 좀 더 풍성하게 해주고. 전에 했던 것처럼 폐차 시안 / 건물 시안 두개 한번에 보여줘.

## 의도·범위

같은 작업 중 사용자 추가 원문:

> 이거, 건물 깨지거나 덩굴 노후화 같은 경우 아예 툴 형식으로 만들어서 에셋들 다 자동으로 되게끔 해주는게 좋아보여. 당장 건물이나 폐차 에셋 하나에만 적용할게 아니라.

이 지시에 따라 특정 모델 한 개의 수정에서 **선택한 정적 환경 메시의 개별/다중 목록 처리, 종류 자동/수동 선택, 형상 절단·단면·크기별 잔해, 재료 노후화, 크기 적응 덩굴, 새 프리팹/프로필 저장**을 하는 재사용 Unity 편집 도구로 확대했다. 스킨/캐릭터나 모든 독자 shader에 대한 보편 변환까지 보장하지 않는다. 같은 목적의 활성 작업 보강이며 새 대표 주제나 중복 완료 회차를 만들지 않는다.

이전04의 재료별 노후화와 재현 가능한 랜덤 덩굴을 유지하고, 메시 형상 자체의 결손·변형·두께 있는 단면·노출 구조와 낙하 위치에 맞는 크기별 파편을 더한다. 폐차와 건물 시안을 각각 보여 준다. 사각 잔해만 둘러놓거나 얼룩 강도만 높인 것을 구조 파손으로 취급하지 않는다.

이번에도 전체 A맵 적용은 하지 않는다. 기존 맵·AI·진입점·가림·캐릭터·renderer·전역 조명·기존 재질·모델 원본을 보존하고 새 사본만 만든다. S06의 회반죽/적벽돌/테라코타 계열과 차량의 청록 도장·금속을 유지한다. 문·큰 창·지붕의 기존 구멍과 파손 단면은 덩굴/파편에 가려 읽히지 않는 상태가 되지 않도록 검토한다.

## 병렬 소유

메인이 설계·Unity 도구 UI/오케스트레이션·통합·전후 촬영, GPT-6 Sol high 서브가 범용 파손 코어, 풍성한 덩굴·저장 처리, 독립 검증을 분리했다. 서브는 staging만 수정하고 메인이 공유 Unity 인계 후 실행했다. 초기 별도 headless Blender 건물 제작물도 보존하지만 최종 시안은 그 전용 모델이 아니라 **같은 범용 Unity API**로 생성했다. 기존 Blender UI 상태를 변경하지 않았다.

## 검증·증빙

작업 전 실제04 사본을 격리 시안에서 촬영하고 같은 카메라·조명·해상도로 변경 후를 촬영한다. 게임 맵 배치나 Play/FPS 검증으로 확대하지 않는다. 원본 해시, body 메시 자체 변화, 파편 크기/접지, 문창/파손 구멍, 덩굴 군집·원래 색, 새 사본 저장/재읽기를 독립 검토한 뒤 완료 및 게시 상태를 갱신한다.

## 실제 변경

- Unity 메뉴 `Escape → Environment → Surface Ruin Tool`에 **폐허 자동 생성** 창을 추가했다. 정적 에셋 다중 목록, 자동/건물/차량/소품 분류, 파손·파편/표면 노후화/덩굴 각각의 켜기와 강도, 고정·새 무늬 시드, 수동 덩굴 보호 영역, 새 프리팹·재사용 프로필 저장을 제공한다.
- 모델 사본에서 실제 삼각형을 절단한다. 저폴리 큰 면이 절단 영역을 가로지르는 경우에만 세분하고, 원본 정점 속성과 재질 구분을 유지한다. 얇은 금속과 건물 단면의 두께를 다르게 하며, 크기와 형태가 다른 잔해를 지면 높이에 둔다. 닫힌 전체 구멍을 평판으로 덮는 방식은 사용하지 않는다.
- 노후화는 기존 재료별 도구를 통합해 지원 재질에 적용한다. 원본 유리·창틀·도장·벽돌·기와를 같은 색으로 치환하지 않는다. 덩굴은 대상의 실제 크기에 맞춰 뿌리·길이·잎 크기와 수를 제한하고, 시드로 모양을 재현한다.
- 각 결과는 격리된 미리보기 씬에 만든다. 새 프리팹 저장 시 생성 메시·재질·텍스처 의존성과 설정을 함께 저장하고 재열어 검사한다. 원본 transient 메시·재질·텍스처도 별도 보호해 미리보기 정리가 원본을 파괴하지 않게 했다. 하나의 대상이 실패해도 목록의 다른 대상은 계속 처리한다.
- 기본 충돌 생성은 OFF다. ON이면 보이는 정적 비-Ivy 메시의 새 비볼록 MeshCollider를 저장한다. 이는 배치 뒤의 게임 이동·NavMesh 검증을 대신하지 않는다.
- 회전·비균일/음수 scale 부모 아래의 선택 오브젝트는 조상 변환 체인을 사본에 보존해 world 행렬을 손실 없이 유지한다.

주요 새 파일은 Unity의 `Assets/Escape/Editor/Environment/` 아래 `SurfaceRuinWindow.cs`, `RuinAuthoring.cs`, `RuinGeometryEngine.cs`, `RuinGeometrySettings.cs`, `RuinIvyRecipes.cs`, `RuinAssetBaker.cs`, `RuinTreatmentProfile.cs` 및 명시 실행 검증기 두 개다. Runtime에는 생성 정보를 보관하는 `RuinTreatmentInstance.cs`만 추가했다. 전체 목록·해시는 `TOOL_SOURCE_HASHES.json`에 있다. 임시 자동 요청 helper 두 개는 최종적으로 Assets에서 제거하고 `integration/`에 증빙용으로 보존한다.

## 시안 오류의 원인과 수정

1. 건물 절단부의 순백 쐐기·흰 빗금은 절단 재질이 아니라 **기존 collider-only proxy 렌더러**였다. `white_diagnostic/`에서 rim 숨김·재질 교체·노후화 제거·stain 숨김으로는 남고, `proxy_diagnostic/`에서 `S06_Shell_colliderOnly`와 `S06_Interior_colliderOnly` 렌더러를 끄자 사라졌다. 범용 도구의 **생성 사본에서만** `colliderOnly/collider_only/collisionProxy` 렌더러를 비활성화했다. 원본은 변경하지 않았다.
2. 사용자 후속 원문: “이거 뭐 공중에 판떼기같은거있는데?” 첨부 실물을 `reference_user_floating_planks.png`로 보존했다. 두 작은 판은 기존 `S06_Interior.fbx`의 `broken_counter_00_plank/01_plank`이며, baseline에도 있었다. 원래 실내 모델은 바닥 메시 없이 두 판과 좁은 받침만 담고 있었다. 최종 건물 시안에서 이 **실내 그룹만 제외**했다. source triangle 26,445→26,397의 48개 차이는 이 정리다. 이를 도구의 자동 파편 처리 성과로 주장하지 않는다. 원본 FBX와 이전 이미지는 보존했다.
3. 초기 candidate02의 큰 적갈색 판은 위 두 판과 별개다. renderer 그룹 숨김·높이 측정으로 생성 잔해이며 지면에 닿아 있음을 확인했다. 부유라는 초기 추정을 정정하고, 이후 잔해를 작고 불규칙한 다면체로 개선했다.

## 최종 채택 시안과 전후 증빙

최종은 **candidate_04**다. `baseline_renders/before_{car|building}_{iso|top|detail}.png` 여섯 장과 `candidate_04/after_...` 여섯 장은 같은 카메라 위치·회전·직교 크기·1600×1000·조명이다. `FINAL_IMAGE_CHECK.json`에서 여섯 카메라가 모두 정확히 일치한다. 변경 전은 실제 작업 전 촬영이며 재구성본이 아니다. 건물의 실내 부품 제외는 위의 별도 시안 정리로 명시한다.

- 폐차: 실제 차체 결손·금속 변형, 금속편 14개, 덩굴 잎 1,206개. 파란 도장·녹·유리/바퀴 공간이 유지된다.
- 건물: 전면 모서리 외벽 결손·단면, 불규칙 잔해 27개, 덩굴 잎 3,305개. 회반죽·적벽돌·기와와 기존 지붕 파손/출입구를 유지한다. 두 카운터 판과 흰 proxy는 최종 시안에서 보이지 않는다.
- `candidate_04/reloaded_...`는 실제 저장 프리팹을 새 미리보기 씬에 재열어 촬영했다. 두 설정 프로필도 저장·재읽기를 통과했다. 경로는 `candidate_04/SAVED_PREFABS.txt`, 적용 설정은 같은 폴더의 car/building recipe JSON에 있다.
- `before_ui_preview.png`/`after_ui_preview.png`는 실제 도구 버튼 생성·저장 확인이다. 스크롤 위치가 다르므로 모델 전후의 같은 시점 비교로 사용하지 않는다. UI 실행 저장물·시드는 `NATIVE_UI_CHECK.json`에 있다. 프로필의 네이티브 저장/로드 버튼은 별도 조작하지 않았고, 프로필 데이터 저장·재읽기는 실행기로 확인했다.
- `before_editor_handoff.png`/`after_editor_handoff.png`는 A맵 자체를 수정하지 않고 원래 편집 화면으로 반환한 증빙이다. native JPEG 원본도 보존한다.

## 검증 결과와 한계

`VALIDATION_215002.txt`의 마지막 결과는 **PASS**다. 차량·건물·소품 실제 형상 변경, 고정 시드 재현/다른 시드 변화, 예산 실패 rollback, 파손0 무생성, 비가독 메시·정점 속성·복합 변환, 원본 transient 리소스 수명, 다중 처리 오류 격리/반복/정리, 프리팹 의존성·설정·시드 재읽기, 적용 노후화 강도0을 확인했다. 외부 부모 변환 검사 오차는0이며, S06 건물 정적 충돌 옵션은 Preview/Bake 재열기 **26개**, Ivy와 충돌용 proxy 제외를 확인했다. 이전 21:46의 7개는 소품 검사이고 S06 검사로 소급하지 않는다.

`IVY_VALIDATION_215238.txt`는 기존 시안 이외 van, Y05 건물, 군용 상자에서 덩굴 크기 적응·생성을 확인했다. 이 세 항목은 덩굴 API 검증이며 세 대상 모두의 최종 아트 승인과 혼동하지 않는다. 독립 검토 원문은 `staging/validation/REVIEW_NOTES.md`이며 candidate04 격리 시안 아트 PASS와 기능 PASS를 구별한다. 중간 실패·정정은 `ITERATION_AUDIT.md`에 남겼다.

`PROTECTED_FINAL.json`은 보호 대상22개(맵 씬/프리팹, AI, 가림, renderer/그래픽 설정, 기존 도구/셰이더와 모델 원본)의 해시가 작업 전과 모두 같음을 확인한다. 새 소스·시안만 추가했다. 마지막 요청 상태에서 Play=false, A맵 dirty=false, Ruin live preview와 미리보기 씬의 Ruin/Ivy/Weather marker가 모두0이다.

지원 범위는 **정적 MeshFilter 환경 에셋**이다. 특수 shader는 건너뛸 수 있고, 한 재질에 합쳐진 유리/문 등의 의미를 완전히 판별하지 못한다. 수동 보호 영역과 외관 확인이 필요하다. 게임 중 동적 파괴, 물리적 붕괴, 전체 A맵 일괄 적용, 실제 Play/FPS/NavMesh/교전 검증은 이번 결과에 포함하지 않는다. 초기 별도 Blender 전용 건물 파일도 범용 Unity 도구 성공의 근거로 사용하지 않는다.

사용 절차는 `staging/ivy/TOOL_GUIDE.md`에 있다. 라이브러리에는 기존 D09 주제의 같은 날05회차로 이어 붙이며, 이전04 기록과 중간 원문을 삭제하거나 덮어쓰지 않는다. 게시 성공은 별도 실제 공개 재조회 영수증으로 확정한다.


## 라이브러리 실제 게시 확인 (2026-10-29 KST)

- 기존 D09 주제의2026-10-29-05 구현 명세 후속으로 연결했다. 맵·레벨 디자인 / 건물·실내·외벽 유지, 앞선01~04와기존기획/원문/이미지/실패기록/당시공개해시를보존하고새대표주제를만들지않았다.
- 허용29문서·22이미지만수집했다. 실제작업전baseline6장과최종candidate04의차량/건물 iso·top·detail6쌍은원본1600×1000 동일카메라로연결했다. 편집기1쌍은원본A맵유지·반환확인이며모델개선/맵적용비교가아니다. 나머지8장은재열기/UI/사용자판자첨부/원인분리진단보조로구별했다. 중간후보이미지를최종after로쓰지않았다.
- 처음편집기2장의reference역할과7번째비교가충돌해담당채팅에서파일/픽셀/선택목록/비교목록은그대로두고역할만before/after로정규화했다. 이정규화완료후manifest와29문서/22이미지hash를동결했고게시중더바꾸지않았다.
- 원문29·이미지22·메인썸네일3·색인·과거ledger고정스냅샷 총56경로를인증없이HTTP200/SHA256일치로재조회했다. 비인증쓰기401유지. 실제D09후속/05메타데이터·분류/28관련문서링크/원문/최종22장이미지·7비교쌍/보조캡션/메인썸네일을확인했다. 원본PNG/실제native원본은보존하고기존웹1440px/품질78·메인400×225이내/품질55기준을유지했다.
- 기존1100항목·55ledger행·원문→게시파일대응표·과거ledger고정링크/앞선A맵공개검증스냅샷·장비07을보존했다. 최종ledger개별항목/원문·색인hash/공개재조회는 PUBLICATION_VERIFICATION.json에저장한다. 동결원문·정규화후manifest는 publication_baseline에보존했다.
- before_library_topic.jpg / after_library_topic.jpg / after_library_detail.jpg / after_library_original.jpg / after_library_main.jpg, BROWSER_PUBLICATION_VERIFICATION.json 및 IMAGE_DELIVERY_VERIFICATION.json은별도로저장한로컬게시증빙이며29문서/22이미지허용목록에자동추가하지않는다.
- 범용정적환경도구의기능검증과최종후보04의두격리시안아트PASS를구별했다. 선택부모변환오차0/S06충돌26은그검사범위로만유지하며van/Y05/상자3종은덩굴API검사다. 시안의기존실내카운터2개제외를범용자동판자정리기능으로과장하지않았다. 초기흰proxy/잔해부유오인/이전Prop7→S06오인정정·중간실패를보존했다.
- 전체A맵적용/Play/FPS/NavMesh/교전/플레이가림·그림자/게임중동적붕괴는검증하지않았다. 정적MeshFilter대상·특수shader/합쳐진재질자동판별한계와수동보호영역·실제게임검증필요를유지했다. 판매자모델/텍스처·코드·DLL·백업바이너리는올리지않았다.
- 게시담당은Unity/Assets/Figma/Blender/원본게임데이터/편집기/인증을실행·변경하지않았다. 사이트UIv18/환경revision9그대로자료만갱신했다.
- 공개주소: https://extraction-design-library-ehoji.ehojin0415.chatgpt.site/#outputs%2Fchange_records%2F2026-10-29%2Fa-map-structural-ruin-studies%2FCHANGE_RECORD.md
