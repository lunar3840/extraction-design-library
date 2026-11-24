---
library_ready: true
library_topic_id: topic:outputs/change_records/2026-10-13/a-map-concept-rebuild-d09
library_category: 맵·레벨 디자인
library_subcategory: 건물·실내·외벽
library_kind: 변경 결과
library_instruction_date: 2026-11-24
library_round_order: 2026-11-24-03
library_round_summary: 시범 블록 세 동의 외형을 사용자가 고른 A안으로 새로 지음. 폐숙소는 복도가 솟은 지붕과 벽돌 보일러실, 옛 식당은 톱날 지붕 홀과 평지붕 주방동, 옛 매점은 간판벽 평지붕 매장과 박공 창고. 주황 기와 상자 3개가 서로 다른 윤곽과 지붕 색으로 바뀜. 여전히 에디터 Play 후보 상태이며 벽 디테일 소품은 아직 없음.
library_publish_status: published
library_published_url: https://lunar3840.github.io/extraction-design-library/#outputs%2Fchange_records%2F2026-11-24%2Fsw-ruins-block-exterior-a%2FCHANGE_RECORD.md
library_publication_receipt: outputs/change_records/2026-11-24/sw-ruins-block-exterior-a/PUBLICATION_VERIFICATION.json
library_publish_reason: GitHub Pages 정적 라이브러리(lunar3840/extraction-design-library)에 기존 D09 주제의 2026-11-24-03 회차로 게시. 후보 상태이며 남은 문제를 그대로 공개.
---

# 시범 블록 외형 A안 (솟은 복도 숙소 · 톱날 지붕 식당 · 간판벽 상가)

## 사용자 지시 요약

- 원문은 [SOURCE_REQUEST.md](SOURCE_REQUEST.md). "너무 블록형태에 주황색 지붕, 단순한 디자인… 실제 사람들이 살았던 공업지대 느낌은 안나긴 해." → 시안 그림을 보고 **"A 괜찮네 전반적으로"**.
- 시안: [EXTERIOR_CONCEPTS_v1.png](EXTERIOR_CONCEPTS_v1.png) (동마다 지금 + A·B·C안).

## 기획 의도와 해결하려는 문제

- 앞 회차(02, 블록 시범)는 실내만 바꿔서 겉은 여전히 같은 상자와 같은 주황 기와였다.
- 쿼터뷰에서는 벽 장식보다 지붕 높낮이와 평면 덩어리가 먼저 읽힌다. 그래서 동마다 지붕 종류와 높이를 다르게 하고, 용도가 밖에서 보이는 덩어리(보일러실, 주방동, 간판벽)를 붙였다.

## 변경 전 상태 (실제 화면)

- `before_01/` 10장 = 앞 회차 최종 상태(sw-ruins-block-pilot/after_04). 실내는 방 구조지만 외벽·지붕은 기존 것(잘라 낸 사본).

## 실제로 바꾼 부분과 방법

- 여전히 후보 상태. 씬·맵 프리팹·기존 스크립트·기존 에셋 수정 0건. `Assets/Escape/Editor/StageASwBlockPilot.cs`만 확장했다(촬영 도구에 한 줄 추가).
- 세 동의 기존 외벽·지붕(BlenderExpansion Shell/Roof, D08_Roof)은 Play 중에만 숨기고, 외벽과 지붕을 코드로 새로 짓는다. 외벽 20여 면(문·창 개구부, 아래 0.9m 침수선 띠, 문턱), 지붕 10개.
  - S01 폐숙소: 가운데 복도를 5m로 높여 고창 줄과 슬레이트 박공을 얹고, 양쪽 방은 낮은 외쪽지붕(바랜 기와색), 남동쪽은 벽돌 보일러실(평지붕 4.4m). 무너진 서쪽 방은 서까래만 남김.
  - S02 옛 식당: 홀은 벽 4.4m에 톱날 지붕 4칸(유리 채광면, 서쪽 첫 칸은 서까래만), 주방동은 낮은 벽돌 평지붕(환기구, 배관).
  - S06 옛 매점: 북쪽에 5.3m 간판벽을 세운 평지붕 매장(천창 3, 물탱크), 뒤는 녹슨 함석 박공 벽돌 창고(남동쪽은 서까래만).
- 새 지붕은 기존 "실내에 들어가면 지붕이 걷히는" 기능이 알지 못한다. 그래서 캐릭터가 건물 안에 있으면 그 동의 지붕과 윗벽을 숨기는 처리를 시범 코드에 넣었다(기존 기능처럼 서서히 사라지지 않고 바로 꺼짐).
- 지붕 색: 슬레이트, 타르 평지붕, 녹슨 함석, 채도를 낮춘 기와색. 벽: 회벽 2색(회녹, 황토)과 붉은 벽돌, 콘크리트.

## 변경 후 결과 (같은 카메라)

- `after_01/` 10장 = sw-ruins-block-pilot/after_06, before와 같은 지점·카메라.
- 원경에서 세 동의 윤곽과 지붕 색이 서로 다르게 읽힌다. 실내 3장은 지붕이 걷힌 상태로 방 구조가 보인다.

## 확인한 것과 확인하지 못한 것

확인함: 촬영 18/18, 컴파일 오류 없음, 건물 안 3지점에서 지붕이 숨겨짐, 촬영 지점에서 캐릭터가 밀려나지 않음.

확인하지 못함: 직접 걸어 다니는 플레이(새 문 통과, 지붕이 켜지고 꺼지는 순간의 느낌), 적 길 찾기, 성능, 환경 툰 룩과 함께 켠 화면, Play 자동 적용 스위치.

## 남은 문제

- 벽이 밋밋하다. 배관·계량기함·방범창·간판 글씨·얼룩 같은 디테일이 아직 없고, 기존 외벽에 있던 담쟁이와 물때 표현도 새 외벽에는 없다.
- 평지붕이 넓은 면으로 보인다(땜질 자국과 환기구만 있음). 지붕은 두께가 없는 판이라 처마 끝이 얇다.
- 지붕이 서서히가 아니라 바로 사라진다. 바깥 창가에 서면 지붕이 시야를 가릴 수 있다.
- 창은 유리 한 장이고 창틀이 단순하다. 문짝이 없다.
- 불합격 보존: `failed_after_05/`(지붕이 걷히지 않아 실내가 안 보임, 기와색이 그대로 주황).

## 다음 단계 제안

- 벽·지붕 디테일 소품을 Tripo로 생성해 붙이기(목록과 크레딧은 사용자 승인 후).
- 사용자가 직접 걸어 보고 판단.
