import { formatRial, tomanLine, type RialAmount } from '../format/money';

interface MoneyProps {
  amount: RialAmount;
  /** The amount a row is about, larger; a second amount in the same row is shown smaller. */
  large?: boolean;
  muted?: boolean;
}

/**
 * Every amount the panel shows: the Rial figure, which is the amount, and under it a quieter
 * line with its Toman equivalent and the Toman in words, so a Rial figure read as Toman, or the
 * other way round, is seen at once (ADR 0002 point 9, ADR 0024).
 */
export function Money({ amount, large = false, muted = false }: MoneyProps) {
  return (
    <span className="flex flex-col">
      <span className={`${large ? 'text-lg font-bold' : 'font-medium'} ${muted ? 'text-stone-400' : 'text-stone-900'}`}>
        {formatRial(amount)}
      </span>
      <span className={`text-xs ${muted ? 'text-stone-400' : 'text-stone-500'}`}>{tomanLine(amount)}</span>
    </span>
  );
}
