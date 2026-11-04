export function ComingSoonPage({ title }: { title: string }) {
  return (
    <section className="flex flex-col gap-2">
      <h1 className="text-[40px] leading-tight font-light tracking-tight">{title}</h1>
      <p className="text-muted-foreground">This screen is built in a later guide.</p>
    </section>
  )
}
