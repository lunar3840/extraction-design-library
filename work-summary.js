/* A navigation aid derived from preserved records, never a completion verdict. */
(()=>{
 const overview={
  'topic:outputs/change_records/2026-10-21/combat-feedback-first-bundle':'명중·피격 표시와 음향을 키워 전투 중 타격과 피해를 더 쉽게 알아보도록 조정.',
  'topic:outputs/change_records/2026-10-19/weapon-ammo-tiers':'총기·탄약의 역할과 장비 제작 기준을 정리하고, 캐릭터 모델·텍스처 전달 자료를 연결.',
  'topic:outputs/change_records/2026-10-23/scenario-chat-archive':'시나리오 Chat의 기획·수정 지시와 참고 이미지를 모아 기존 주제의 후속 기록으로 보존.',
  'topic:chat/6a9d0fc0-0d14-83ee-b952-0959548603e3':'에델의 얼굴·리깅·애니메이션을 다듬고, 총기마다 다른 파지·조준 자세를 검토.',
  'topic:chat/6a9cfb2c-d738-83e8-92f4-a2f5fee2435f':'주인공 세린의 외형·얼굴·리깅과 애니메이션 제작·수정 요청을 정리.',
  'topic:chat/6ad822ac-0b28-83ee-92f6-de3cfe7e06da':'리브 모델의 눈꺼풀·표정 수정과 얼굴·몸 애니메이션 작업 내역을 정리.',
  'topic:chat/6a9aa012-2568-83ee-b24a-38588f3e743a':'게임의 세계관과 파밍 재화 설정을 통합하고 후속 수정 방향을 정리.'
 };
 function clean(value){return String(value||'').replace(/!\[[^\]]*\]\([^)]*\)/g,'').replace(/\[([^\]]+)\]\([^)]*\)/g,'$1').replace(/^[#\s>*-]+/gm,'').replace(/\blibrary_\w+:.*$/gm,'').replace(/[`*_]/g,'').replace(/\s+/g,' ').trim();}
 function short(value){let text=clean(value);const sentence=text.match(/^(.+?[가-힣][.!?])(?:\s|$)/)?.[1];if(sentence&&sentence.length>=18)text=sentence;if(text.length>88){let end=text.slice(0,85).search(/\s+\S*$/);text=text.slice(0,end>55?end:85).replace(/[\s,·;:]+$/,'')+'…';}return text;}
 const meaningful=value=>{const t=clean(value);return t.length>=12&&!/^(응|좋아|부탁|계속|다시|하고 있|해봐|해줘)[.!?\s]*$/.test(t);};
 function topic(g,rows){const latest=[...rows].sort((a,b)=>(b.library?.round_order||b.date||'').localeCompare(a.library?.round_order||a.date||''));const explicit=latest.find(r=>meaningful(r.library?.card_summary))?.library.card_summary;if(explicit)return short(explicit);if(overview[g.id])return overview[g.id];const candidate=[g.summary,...latest.map(r=>r.library?.round_summary),...latest.map(r=>r.title)].find(meaningful);return short(candidate||g.title+' 관련 기획과 수정 기록을 확인할 수 있습니다.');}
 function record(r){return short(r.library?.card_summary||r.library?.round_summary||r.title);}
 window.LIBRARY_WORK_SUMMARY={topic,record};
})();
