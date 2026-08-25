import logo from '../../assets/aits-logo.png'

export function Brand() {
  return (
    <div className="flex items-center gap-3">
      <img src={logo} alt="AITS logo" className="h-11 w-11 rounded-full border border-[#e1cfad] bg-[#fefdf9] object-cover" />
      <div>
        <div className="text-[15px] font-bold tracking-[-0.02em] text-[#241b13]">Document Review</div>
        <div className="text-[9px] font-bold uppercase tracking-[0.17em] text-[#a06d27]">Analysis workspace</div>
      </div>
    </div>
  )
}
