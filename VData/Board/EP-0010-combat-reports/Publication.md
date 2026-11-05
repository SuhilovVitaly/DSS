# EP-0010 — ticket → commit → push

Ветка `base-fight`, remote `origin` = `https://github.com/SuhilovVitaly/DSS`. Исходный HEAD `af594de8465421f45cd362e82c6c1dbfdd8631ed`. Один commit и немедленный push каждого тикета; без rewrite/force-push. Порядок ниже — фактический, corrective TK04/05 выполнены перед native TK03, TK06 — после него. US11TK03 содержит evidence и открытую приёмку, не PASS всех native gates.

| Ticket | Commit SHA | Push |
|---|---|---|
| Preparation | `f6cd7ea41a705aca7d6c25689c96de5bc35f1306` | PUSHED; remote SHA verified before next ticket |
| US01/TK01 | `a49debeb6a6df8b01b150487b6d002420a53cfcd` | PUSHED; remote SHA verified before next ticket |
| US01/TK02 | `e944f2827c7ab294e6e7b05c153a5236fd4650ac` | PUSHED; remote SHA verified before next ticket |
| US02/TK01 | `5f682d2dfe8e47ba5dda007390e12c31c3e3b2ad` | PUSHED; remote SHA verified before next ticket |
| US02/TK02 | `00d3c9d8afdfe6ba992e126bffc68c207ccd06fc` | PUSHED; remote SHA verified before next ticket |
| US03/TK01 | `4b6b1d0d6419ff51eef8c026ad9d300a2645e0ac` | PUSHED; remote SHA verified before next ticket |
| US03/TK02 | `5cf06b1a04ec892aab59a7a380fc030eb1aa50ca` | PUSHED; remote SHA verified before next ticket |
| US04/TK01 | `caadaa025d86a6388ac1a714092cc81a87aa70d6` | PUSHED; remote SHA verified before next ticket |
| US04/TK02 | `518efe5495721825e2fefc562ad5004a7f345fe5` | PUSHED; remote SHA verified before next ticket |
| US05/TK01 | `5ad677422f317281f0630e7a0a99bede6ff764cd` | PUSHED; remote SHA verified before next ticket |
| US05/TK02 | `9900015d413b898af49b58ee18b6896b0905d390` | PUSHED; remote SHA verified before next ticket |
| US06/TK01 | `8cbb8ad96bd08508ae76b31b76a06c4cab5130e7` | PUSHED; remote SHA verified before next ticket |
| US06/TK02 | `344aae55db58a91dd60253e1217d51756c89bdb5` | PUSHED; remote SHA verified before next ticket |
| US07/TK01 | `0eb073a59c85646889538f586cccef3eb7481c84` | PUSHED; remote SHA verified before next ticket |
| US07/TK02 | `03eccb6d7a51092fe2068f5155621f67d297a431` | PUSHED; remote SHA verified before next ticket |
| US08/TK01 | `b320f74850b6ba70388ee4d24fa35bca1265d202` | PUSHED; remote SHA verified before next ticket |
| US08/TK02 | `844942856d29d1f5c745ca5c25b369d7b696f1d3` | PUSHED; remote SHA verified before next ticket |
| US08/TK03 | `ce47dac2100b2d5d7cccfac4e7efdf2d7c449214` | PUSHED; remote SHA verified before next ticket |
| US08/TK04 | `acfdce903df3e7829a40b234fe1f7e35048cced9` | PUSHED; remote SHA verified before next ticket |
| US09/TK01 | `7a62c7b7554170a7f9c50c590a8623101c64022d` | PUSHED; remote SHA verified before next ticket |
| US09/TK02 | `31b83e0b22948ddd645ae9689a7364b14086510a` | PUSHED; remote SHA verified before next ticket |
| US10/TK01 | `d2a177051d9c430ff00a628fd641a521c8626b18` | PUSHED; remote SHA verified before next ticket |
| US10/TK02 | `6ff6199e32f7cc9bcab2a040e074e9d09cfaca93` | PUSHED; remote SHA verified before next ticket |
| US10/TK03 | `81fa61f0a593b909719563c16bd6cef839b297d8` | PUSHED; remote SHA verified before next ticket |
| US11/TK01 | `276bc6331e89b65412713ccf02eaf2840cc9f186` | PUSHED; remote SHA verified before next ticket |
| US11/TK02 | `9f022367fec29f41f15e8cb20c9a1f232bf26b02` | PUSHED; remote SHA verified before next ticket |
| US11/TK04 | `cfb91938c2c1409213d535911cc11e7ed73e7bf6` | PUSHED; remote SHA verified before next ticket |
| US11/TK05 | `47b60d2902c8577642d51af3894682104b9673f9` | PUSHED; remote SHA verified before next ticket |
| US11/TK03 | `5d2d9357a4c08b303ce7285ee1bfa7178f5346bd` | PUSHED; remote SHA verified before next ticket |
| US11/TK06 | `a1a4dd1f6716ed77699daae4d7c971e0ccd218fb` | PUSHED; remote SHA verified before next ticket |
| US12/TK01 | Финальный documentation commit, содержащий этот файл; SHA проверяется Git после создания | Отдельный обязательный push и exact remote SHA check перед итоговым ответом |

Собственный SHA текущего финального коммита не записывается заранее. Проверка после публикации: `git rev-parse HEAD` и `git ls-remote origin refs/heads/base-fight`; оба должны совпасть. Полные SHA остальных29 тикетов выше;30-й — финальная документация.
