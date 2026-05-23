import { InlineScript } from "@/components/inline-script";
import { SELLER_THEME_STORAGE_KEY } from "@/lib/theme";

const THEME_INIT = `(function(){
  try {
    var k = '${SELLER_THEME_STORAGE_KEY}';
    var t = localStorage.getItem(k);
    var root = document.documentElement;
    if (t === 'dark') root.classList.add('dark');
    else root.classList.remove('dark');
  } catch (e) {}
})();`;

export function ThemeScript() {
  return <InlineScript html={THEME_INIT} />;
}
