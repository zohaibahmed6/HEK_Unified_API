/** Shared helpers for turning raw legacy responses (HISO/ERMS/KARO) into readable sections instead of JSON dumps. */

export function prettyLabel(name: string): string {
  return name
    .replace(/^(Patient_|CurrentUser_|CurrentUserOrganisation_|TargetPractitioner_|TargetOrganisation_)/, "")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^./, (c) => c.toUpperCase());
}

export function KeyValue({ pairs }: { pairs: [string, string | undefined | null][] }) {
  return (
    <dl className="hiso-field-list">
      {pairs.map(([label, value]) => (
        <div className="hiso-field-row" key={label}>
          <dt>{label}</dt>
          <dd>{value ? value : <span className="hiso-empty-val">—</span>}</dd>
        </div>
      ))}
    </dl>
  );
}
