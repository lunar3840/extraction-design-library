/* Calendar dates from preserved records, never filesystem modification times. */
(()=>{
 function day(value){
  if(typeof value!=='string'||!/^\d{4}-\d{2}-\d{2}$/.test(value))return '';
  const parsed=new Date(value+'T00:00:00Z');
  return Number.isFinite(parsed.valueOf())&&parsed.toISOString().slice(0,10)===value?value:'';
 }
 const recordDay=r=>day(r.library?.instruction_date)||day(r.date);
 function bounds(rows){const dates=rows.map(recordDay).filter(Boolean).sort();return {first:dates[0]||'',last:dates.at(-1)||'',undated:rows.length-dates.length};}
 function compare(a,b,direction='newest'){
  const x=direction==='oldest'?a.first:a.last,y=direction==='oldest'?b.first:b.last;
  if(!x||!y){if(x!==y)return x?-1:1;}
  const dateOrder=direction==='oldest'?x.localeCompare(y):y.localeCompare(x);
  return dateOrder||String(a.title||'').localeCompare(String(b.title||''),'ko')||String(a.id).localeCompare(String(b.id));
 }
 const records=(rows,direction)=>rows.map(r=>({...bounds([r]),id:r.id,title:r.title,record:r})).sort((a,b)=>compare(a,b,direction)).map(r=>r.record);
 window.LIBRARY_CHRONOLOGY={day,recordDay,bounds,compare,records};
})();
